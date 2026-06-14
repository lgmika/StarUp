using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Nda.Dtos;
using StartupConnect.Application.Nda.Interfaces;
using StartupConnect.Application.Realtime;
using StartupConnect.Application.Realtime.Interfaces;
using StartupConnect.Domain.Constants;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.Nda;

public sealed class NdaService(
    AppDbContext dbContext,
    IRealtimeNotifier realtimeNotifier) : INdaService
{
    public async Task<IReadOnlyCollection<NdaTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
        var templates = await dbContext.NdaTemplates
            .Include(template => template.Versions)
            .OrderBy(template => template.Name)
            .ToArrayAsync(cancellationToken);

        return templates.Select(MapTemplate).ToArray();
    }

    public async Task<NdaTemplateDto> CreateTemplateAsync(ClaimsPrincipal principal, CreateNdaTemplateRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureAdminAsync(userId, cancellationToken);
        ValidateRequired(request.Name, "name", "NDA template name is required");
        ValidateRequired(request.InitialContent, "initialContent", "Initial NDA content is required");

        var template = new NdaTemplate
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? string.Empty : request.Description.Trim(),
            IsActive = true
        };

        template.Versions.Add(new NdaTemplateVersion
        {
            Template = template,
            VersionNumber = 1,
            Content = request.InitialContent.Trim(),
            IsPublished = true
        });

        dbContext.NdaTemplates.Add(template);
        AddAudit(userId, "NDA.Template.Create", "NdaTemplate", template.Id, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapTemplate(template);
    }

    public async Task<NdaTemplateVersionDto> CreateTemplateVersionAsync(ClaimsPrincipal principal, Guid templateId, CreateNdaTemplateVersionRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureAdminAsync(userId, cancellationToken);
        ValidateRequired(request.Content, "content", "NDA content is required");

        var template = await dbContext.NdaTemplates.Include(item => item.Versions)
            .FirstOrDefaultAsync(item => item.Id == templateId, cancellationToken)
            ?? throw new ApiException("NDA template not found", HttpStatusCode.NotFound);

        var nextVersion = template.Versions.Count == 0 ? 1 : template.Versions.Max(version => version.VersionNumber) + 1;
        var version = new NdaTemplateVersion
        {
            TemplateId = templateId,
            VersionNumber = nextVersion,
            Content = request.Content.Trim(),
            IsPublished = true
        };

        dbContext.NdaTemplateVersions.Add(version);
        AddAudit(userId, "NDA.TemplateVersion.Create", "NdaTemplate", templateId, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapVersion(version);
    }

    public async Task<CurrentProjectNdaDto> GetCurrentProjectNdaAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var setting = await dbContext.ProjectVisibilitySettings.FirstOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken)
            ?? throw new ApiException("Project visibility setting not found", HttpStatusCode.NotFound);

        if (!setting.RequiresNda)
        {
            return new CurrentProjectNdaDto(projectId, false, null, null, null, null, false);
        }

        var version = await GetLatestActiveVersionAsync(cancellationToken);
        var accepted = await dbContext.NdaAgreements.AnyAsync(
            agreement => agreement.ProjectId == projectId && agreement.UserId == userId && agreement.TemplateVersionId == version.Id,
            cancellationToken);

        return new CurrentProjectNdaDto(projectId, true, version.TemplateId, version.Id, version.VersionNumber, version.Content, accepted);
    }

    public async Task<NdaAgreementDto> AcceptProjectNdaAsync(ClaimsPrincipal principal, Guid projectId, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var project = await dbContext.Projects.FirstOrDefaultAsync(item => item.Id == projectId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);

        var setting = await dbContext.ProjectVisibilitySettings.FirstOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken)
            ?? throw new ApiException("Project visibility setting not found", HttpStatusCode.NotFound);

        if (!setting.RequiresNda)
        {
            throw new ApiException("Project does not require NDA", HttpStatusCode.BadRequest);
        }

        var version = await GetLatestActiveVersionAsync(cancellationToken);
        var existing = await dbContext.NdaAgreements.FirstOrDefaultAsync(
            agreement => agreement.ProjectId == projectId && agreement.UserId == userId && agreement.TemplateVersionId == version.Id,
            cancellationToken);

        if (existing is not null)
        {
            return MapAgreement(existing);
        }

        var agreement = new NdaAgreement
        {
            ProjectId = projectId,
            UserId = userId,
            TemplateId = version.TemplateId,
            TemplateVersionId = version.Id,
            VersionNumber = version.VersionNumber,
            AgreementSnapshot = version.Content,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            AcceptedAt = DateTimeOffset.UtcNow
        };

        dbContext.NdaAgreements.Add(agreement);

        var pendingInterest = await dbContext.InvestorProjectInterests.FirstOrDefaultAsync(
            interest => interest.ProjectId == projectId &&
                interest.InvestorUserId == userId &&
                interest.Status == InvestorInterestStatus.AcceptedPendingNda,
            cancellationToken);

        if (pendingInterest is not null)
        {
            pendingInterest.Status = InvestorInterestStatus.Accepted;
            pendingInterest.UpdatedAt = DateTimeOffset.UtcNow;
            pendingInterest.DecidedAt ??= DateTimeOffset.UtcNow;

            var grantExists = await dbContext.ProjectAccessGrants.AnyAsync(
                grant => grant.ProjectId == projectId && grant.UserId == userId,
                cancellationToken);

            if (!grantExists)
            {
                dbContext.ProjectAccessGrants.Add(new ProjectAccessGrant
                {
                    ProjectId = projectId,
                    UserId = userId,
                    AccessLevel = "Investor"
                });
            }
        }

        AddNotification(project.OwnerUserId, "NDA accepted", "A user accepted the project NDA.", agreement.Id, "NdaAgreement");
        AddAudit(userId, "NDA.Accept", "Project", projectId, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = MapAgreement(agreement);
        await realtimeNotifier.NotifyProjectAsync(projectId, RealtimeEventNames.NdaAgreementAccepted, result, cancellationToken);
        await realtimeNotifier.NotifyUserAsync(project.OwnerUserId, RealtimeEventNames.NdaAgreementAccepted, result, cancellationToken);
        if (pendingInterest is not null)
        {
            var interestDto = await dbContext.InvestorProjectInterests
                .Include(interest => interest.Project)
                .Include(interest => interest.InvestorUser)
                .Where(interest => interest.Id == pendingInterest.Id)
                .Select(interest => new
                {
                    interest.Id,
                    interest.ProjectId,
                    ProjectTitle = interest.Project.Title,
                    interest.InvestorUserId,
                    InvestorEmail = interest.InvestorUser.Email,
                    interest.Message,
                    interest.Status,
                    interest.FounderResponse,
                    interest.CreatedAt,
                    interest.UpdatedAt
                })
                .FirstAsync(cancellationToken);

            await realtimeNotifier.InvestorInterestChangedAsync(projectId, userId, interestDto, cancellationToken);
        }

        return result;
    }

    public async Task<IReadOnlyCollection<NdaAgreementDto>> GetProjectAgreementsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (!await IsAdminAsync(userId, cancellationToken))
        {
            await EnsureCanManageProjectAsync(projectId, userId, cancellationToken);
        }

        var agreements = await dbContext.NdaAgreements
            .Where(agreement => agreement.ProjectId == projectId)
            .OrderByDescending(agreement => agreement.AcceptedAt)
            .ToArrayAsync(cancellationToken);

        return agreements.Select(MapAgreement).ToArray();
    }

    public async Task<IReadOnlyCollection<NdaAgreementDto>> GetMyAgreementsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var agreements = await dbContext.NdaAgreements
            .Where(agreement => agreement.UserId == userId)
            .OrderByDescending(agreement => agreement.AcceptedAt)
            .ToArrayAsync(cancellationToken);

        return agreements.Select(MapAgreement).ToArray();
    }

    private async Task<NdaTemplateVersion> GetLatestActiveVersionAsync(CancellationToken cancellationToken)
    {
        return await dbContext.NdaTemplateVersions
            .Include(version => version.Template)
            .Where(version => version.IsPublished && version.Template.IsActive)
            .OrderByDescending(version => version.CreatedAt)
            .ThenByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ApiException("No active NDA template version found", HttpStatusCode.NotFound);
    }

    private async Task EnsureCanManageProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        var canManage = await dbContext.ProjectMembers.AnyAsync(
            member => member.ProjectId == projectId &&
                member.UserId == userId &&
                member.IsActive &&
                (member.Role == ProjectMemberRole.Founder || member.Role == ProjectMemberRole.CoFounder),
            cancellationToken);

        if (!canManage)
        {
            throw new ApiException("You do not have permission to view project NDA agreements", HttpStatusCode.Forbidden);
        }
    }

    private async Task EnsureAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync(userId, cancellationToken))
        {
            throw new ApiException("Admin role is required", HttpStatusCode.Forbidden);
        }
    }

    private async Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserRoles
            .Include(userRole => userRole.Role)
            .AnyAsync(userRole => userRole.UserId == userId && userRole.Role.Code == SystemRoles.Admin, cancellationToken);
    }

    private static NdaTemplateDto MapTemplate(NdaTemplate template)
    {
        return new NdaTemplateDto(
            template.Id,
            template.Name,
            template.Description,
            template.IsActive,
            template.Versions.OrderByDescending(version => version.VersionNumber).Select(MapVersion).ToArray());
    }

    private static NdaTemplateVersionDto MapVersion(NdaTemplateVersion version)
    {
        return new NdaTemplateVersionDto(version.Id, version.TemplateId, version.VersionNumber, version.Content, version.IsPublished, version.CreatedAt);
    }

    private static NdaAgreementDto MapAgreement(NdaAgreement agreement)
    {
        return new NdaAgreementDto(
            agreement.Id,
            agreement.ProjectId,
            agreement.UserId,
            agreement.TemplateId,
            agreement.TemplateVersionId,
            agreement.VersionNumber,
            agreement.AcceptedAt,
            agreement.IpAddress,
            agreement.UserAgent);
    }

    private void AddNotification(Guid userId, string title, string message, Guid resourceId, string resourceType)
    {
        dbContext.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = NotificationType.System,
            Title = title,
            Message = message,
            ResourceId = resourceId,
            ResourceType = resourceType
        });
    }

    private void AddAudit(Guid actorUserId, string action, string resourceType, Guid resourceId, string? reason)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
        });
    }

    private static void ValidateRequired(string? value, string field, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException([new ErrorDetail("Required", message, field)]);
        }
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var userIdValue =
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            principal.FindFirst("sub")?.Value ??
            principal.FindFirst("nameid")?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new ApiException("Invalid access token", HttpStatusCode.Unauthorized);
        }

        return userId;
    }
}
