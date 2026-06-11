using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Investors.Dtos;
using StartupConnect.Application.Investors.Interfaces;
using StartupConnect.Application.Projects.Dtos;
using StartupConnect.Domain.Constants;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.Investors;

public sealed class InvestorService(AppDbContext dbContext) : IInvestorService
{
    public async Task<InvestorProfileDto> GetMyProfileAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var profile = await dbContext.InvestorProfiles.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new ApiException("Investor profile not found", HttpStatusCode.NotFound);

        return MapProfile(profile);
    }

    public async Task<InvestorProfileDto> CreateProfileAsync(ClaimsPrincipal principal, UpsertInvestorProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureInvestorRoleAsync(userId, cancellationToken);
        ValidateProfile(request);

        var exists = await dbContext.InvestorProfiles.AnyAsync(profile => profile.UserId == userId, cancellationToken);
        if (exists)
        {
            throw new ApiException("Investor profile already exists", HttpStatusCode.Conflict);
        }

        var profile = new InvestorProfile
        {
            UserId = userId,
            DisplayName = request.DisplayName.Trim(),
            OrganizationName = TrimOrNull(request.OrganizationName),
            Bio = TrimOrNull(request.Bio),
            InvestmentFocus = TrimOrNull(request.InvestmentFocus),
            WebsiteUrl = TrimOrNull(request.WebsiteUrl),
            LinkedInUrl = TrimOrNull(request.LinkedInUrl),
            MinTicketSize = request.MinTicketSize,
            MaxTicketSize = request.MaxTicketSize
        };

        dbContext.InvestorProfiles.Add(profile);
        AddAudit(userId, "InvestorProfile.Create", "InvestorProfile", profile.Id, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapProfile(profile);
    }

    public async Task<InvestorProfileDto> UpdateProfileAsync(ClaimsPrincipal principal, UpsertInvestorProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureInvestorRoleAsync(userId, cancellationToken);
        ValidateProfile(request);

        var profile = await dbContext.InvestorProfiles.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new ApiException("Investor profile not found", HttpStatusCode.NotFound);

        profile.DisplayName = request.DisplayName.Trim();
        profile.OrganizationName = TrimOrNull(request.OrganizationName);
        profile.Bio = TrimOrNull(request.Bio);
        profile.InvestmentFocus = TrimOrNull(request.InvestmentFocus);
        profile.WebsiteUrl = TrimOrNull(request.WebsiteUrl);
        profile.LinkedInUrl = TrimOrNull(request.LinkedInUrl);
        profile.MinTicketSize = request.MinTicketSize;
        profile.MaxTicketSize = request.MaxTicketSize;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        AddAudit(userId, "InvestorProfile.Update", "InvestorProfile", profile.Id, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapProfile(profile);
    }

    public async Task<IReadOnlyCollection<InvestorProjectDiscoveryDto>> GetInvestorProjectsAsync(ClaimsPrincipal principal, string? search, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureInvestorRoleAsync(userId, cancellationToken);

        var query =
            from project in dbContext.Projects
            join setting in dbContext.ProjectVisibilitySettings on project.Id equals setting.ProjectId
            where !project.IsDeleted &&
                  project.Status == ProjectStatus.Published &&
                  (setting.Visibility == ProjectVisibility.Public ||
                   setting.Visibility == ProjectVisibility.Limited ||
                   setting.Visibility == ProjectVisibility.InvestorOnly)
            select new { project, setting.Visibility };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLowerInvariant();
            query = query.Where(item =>
                item.project.Title.ToLower().Contains(keyword) ||
                item.project.Summary.ToLower().Contains(keyword));
        }

        var items = await query.OrderByDescending(item => item.project.CreatedAt).ToArrayAsync(cancellationToken);
        return items.Select(item => new InvestorProjectDiscoveryDto(
            new ProjectSummaryDto(
                item.project.Id,
                item.project.Title,
                item.project.Slug,
                item.project.Summary,
                item.project.Status,
                item.project.Stage,
                item.Visibility,
                item.project.IsRecruiting,
                item.project.CreatedAt),
            $"Mock investor summary: {item.project.Title} is at {item.project.Stage} stage. Review market clarity, team needs, traction, and funding use."))
            .ToArray();
    }

    public async Task<InvestorInterestDto> CreateInterestAsync(ClaimsPrincipal principal, Guid projectId, CreateInvestorInterestRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureInvestorRoleAsync(userId, cancellationToken);
        ValidateRequired(request.Message, "message", "Investor interest message is required");

        var project = await dbContext.Projects.FirstOrDefaultAsync(item => item.Id == projectId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);

        if (project.Status != ProjectStatus.Published)
        {
            throw new ApiException("Investor interest can only be sent for published projects", HttpStatusCode.BadRequest);
        }

        if (project.OwnerUserId == userId)
        {
            throw new ApiException("Project owner cannot send investor interest to own project", HttpStatusCode.Conflict);
        }

        var duplicate = await dbContext.InvestorProjectInterests.AnyAsync(
            interest => interest.ProjectId == projectId && interest.InvestorUserId == userId,
            cancellationToken);

        if (duplicate)
        {
            throw new ApiException("Investor interest already exists for this project", HttpStatusCode.Conflict);
        }

        var interest = new InvestorProjectInterest
        {
            ProjectId = projectId,
            InvestorUserId = userId,
            Message = request.Message.Trim(),
            Status = InvestorInterestStatus.Pending
        };

        dbContext.InvestorProjectInterests.Add(interest);
        AddNotification(project.OwnerUserId, "New investor interest", $"An investor is interested in {project.Title}.", interest.Id, "InvestorProjectInterest");
        AddAudit(userId, "InvestorInterest.Create", "InvestorProjectInterest", interest.Id, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetInterestDtoAsync(interest.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<InvestorInterestDto>> GetMyInterestsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        return await QueryInterests()
            .Where(interest => interest.InvestorUserId == userId)
            .OrderByDescending(interest => interest.CreatedAt)
            .Select(interest => MapInterest(interest))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<InvestorInterestDto>> GetProjectInterestsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureCanManageProjectAsync(projectId, userId, cancellationToken);

        return await QueryInterests()
            .Where(interest => interest.ProjectId == projectId)
            .OrderByDescending(interest => interest.CreatedAt)
            .Select(interest => MapInterest(interest))
            .ToArrayAsync(cancellationToken);
    }

    public Task<InvestorInterestDto> AcceptInterestAsync(ClaimsPrincipal principal, Guid projectId, Guid interestId, InvestorInterestDecisionRequest request, CancellationToken cancellationToken)
    {
        return FounderDecisionAsync(principal, projectId, interestId, InvestorInterestStatus.Accepted, request, grantAccess: true, cancellationToken);
    }

    public Task<InvestorInterestDto> RejectInterestAsync(ClaimsPrincipal principal, Guid projectId, Guid interestId, InvestorInterestDecisionRequest request, CancellationToken cancellationToken)
    {
        return FounderDecisionAsync(principal, projectId, interestId, InvestorInterestStatus.Rejected, request, grantAccess: false, cancellationToken);
    }

    public Task<InvestorInterestDto> RequestMoreInfoAsync(ClaimsPrincipal principal, Guid projectId, Guid interestId, InvestorInterestDecisionRequest request, CancellationToken cancellationToken)
    {
        return FounderDecisionAsync(principal, projectId, interestId, InvestorInterestStatus.NeedMoreInfo, request, grantAccess: false, cancellationToken);
    }

    public async Task<InvestorInterestDto> WithdrawInterestAsync(ClaimsPrincipal principal, Guid projectId, Guid interestId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var interest = await dbContext.InvestorProjectInterests.FirstOrDefaultAsync(
            item => item.Id == interestId && item.ProjectId == projectId,
            cancellationToken)
            ?? throw new ApiException("Investor interest not found", HttpStatusCode.NotFound);

        if (interest.InvestorUserId != userId)
        {
            throw new ApiException("Only the investor can withdraw this interest", HttpStatusCode.Forbidden);
        }

        if (interest.Status is InvestorInterestStatus.Accepted or InvestorInterestStatus.Rejected or InvestorInterestStatus.Withdrawn or InvestorInterestStatus.Closed)
        {
            throw new ApiException("Interest cannot be withdrawn from current status", HttpStatusCode.BadRequest);
        }

        interest.Status = InvestorInterestStatus.Withdrawn;
        interest.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(userId, "InvestorInterest.Withdraw", "InvestorProjectInterest", interest.Id, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetInterestDtoAsync(interest.Id, cancellationToken);
    }

    private async Task<InvestorInterestDto> FounderDecisionAsync(
        ClaimsPrincipal principal,
        Guid projectId,
        Guid interestId,
        InvestorInterestStatus requestedStatus,
        InvestorInterestDecisionRequest request,
        bool grantAccess,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureCanManageProjectAsync(projectId, userId, cancellationToken);

        var interest = await dbContext.InvestorProjectInterests
            .Include(item => item.Project)
            .FirstOrDefaultAsync(item => item.Id == interestId && item.ProjectId == projectId, cancellationToken)
            ?? throw new ApiException("Investor interest not found", HttpStatusCode.NotFound);

        if (interest.Status is InvestorInterestStatus.Accepted or InvestorInterestStatus.Rejected or InvestorInterestStatus.Withdrawn or InvestorInterestStatus.Closed)
        {
            throw new ApiException("Interest cannot be changed from current status", HttpStatusCode.BadRequest);
        }

        var nextStatus = requestedStatus;
        if (requestedStatus == InvestorInterestStatus.Accepted)
        {
            var setting = await dbContext.ProjectVisibilitySettings.FirstAsync(item => item.ProjectId == projectId, cancellationToken);
            nextStatus = setting.RequiresNda ? InvestorInterestStatus.AcceptedPendingNda : InvestorInterestStatus.Accepted;
        }

        interest.Status = nextStatus;
        interest.FounderResponse = TrimOrNull(request.Response);
        interest.DecidedAt = DateTimeOffset.UtcNow;
        interest.UpdatedAt = DateTimeOffset.UtcNow;

        if (grantAccess && nextStatus == InvestorInterestStatus.Accepted)
        {
            var exists = await dbContext.ProjectAccessGrants.AnyAsync(
                grant => grant.ProjectId == projectId && grant.UserId == interest.InvestorUserId,
                cancellationToken);

            if (!exists)
            {
                dbContext.ProjectAccessGrants.Add(new ProjectAccessGrant
                {
                    ProjectId = projectId,
                    UserId = interest.InvestorUserId,
                    AccessLevel = "Investor"
                });
            }
        }

        AddNotification(interest.InvestorUserId, "Investor interest updated", $"Your investor interest status is now {nextStatus}.", interest.Id, "InvestorProjectInterest");
        AddAudit(userId, $"InvestorInterest.{nextStatus}", "InvestorProjectInterest", interest.Id, request.Response);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetInterestDtoAsync(interest.Id, cancellationToken);
    }

    private IQueryable<InvestorProjectInterest> QueryInterests()
    {
        return dbContext.InvestorProjectInterests
            .Include(interest => interest.Project)
            .Include(interest => interest.InvestorUser);
    }

    private async Task<InvestorInterestDto> GetInterestDtoAsync(Guid interestId, CancellationToken cancellationToken)
    {
        var interest = await QueryInterests().FirstAsync(item => item.Id == interestId, cancellationToken);
        return MapInterest(interest);
    }

    private static InvestorInterestDto MapInterest(InvestorProjectInterest interest)
    {
        return new InvestorInterestDto(
            interest.Id,
            interest.ProjectId,
            interest.Project.Title,
            interest.InvestorUserId,
            interest.InvestorUser.Email,
            interest.Message,
            interest.Status,
            interest.FounderResponse,
            interest.CreatedAt,
            interest.UpdatedAt);
    }

    private static InvestorProfileDto MapProfile(InvestorProfile profile)
    {
        return new InvestorProfileDto(
            profile.Id,
            profile.UserId,
            profile.DisplayName,
            profile.OrganizationName,
            profile.Bio,
            profile.InvestmentFocus,
            profile.WebsiteUrl,
            profile.LinkedInUrl,
            profile.MinTicketSize,
            profile.MaxTicketSize);
    }

    private async Task EnsureInvestorRoleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var isInvestor = await dbContext.UserRoles
            .Include(userRole => userRole.Role)
            .AnyAsync(userRole => userRole.UserId == userId && userRole.Role.Code == SystemRoles.Investor, cancellationToken);

        if (!isInvestor)
        {
            throw new ApiException("Investor role is required", HttpStatusCode.Forbidden);
        }
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
            throw new ApiException("You do not have permission to manage investor interests", HttpStatusCode.Forbidden);
        }
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
            Reason = TrimOrNull(reason)
        });
    }

    private static void ValidateProfile(UpsertInvestorProfileRequest request)
    {
        ValidateRequired(request.DisplayName, "displayName", "Display name is required");

        if (request.MinTicketSize is not null && request.MaxTicketSize is not null && request.MinTicketSize > request.MaxTicketSize)
        {
            throw new ValidationException([new ErrorDetail("InvalidTicketRange", "Min ticket size must be less than or equal to max ticket size", "minTicketSize")]);
        }
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

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

