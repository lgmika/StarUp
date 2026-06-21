using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StartupConnect.Application.Applications.Dtos;
using StartupConnect.Application.Applications.Interfaces;
using StartupConnect.Application.Admin.Interfaces;
using StartupConnect.Application.Realtime.Interfaces;
using StartupConnect.Domain.Constants;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Infrastructure.Notifications;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.Applications;

public sealed class ApplicationService(
    AppDbContext dbContext,
    IRealtimeNotifier realtimeNotifier,
    ISystemSettingReader systemSettingReader) : IApplicationService
{
    public async Task<ApplicationDto> ApplyAsync(ClaimsPrincipal principal, Guid projectId, ApplyProjectRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        ValidateCoverLetter(request.CoverLetter);
        await EnsureVerifiedAsync(userId, cancellationToken);

        var project = await dbContext.Projects.FirstOrDefaultAsync(item => item.Id == projectId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);

        if (project.Status != ProjectStatus.Published)
        {
            throw new ApiException("Only published projects accept applications", HttpStatusCode.BadRequest);
        }

        if (!project.IsRecruiting)
        {
            throw new ApiException("Project is not recruiting", HttpStatusCode.BadRequest);
        }

        var isMemberOrFounder = await dbContext.ProjectMembers.AnyAsync(
            member => member.ProjectId == projectId && member.UserId == userId && member.IsActive,
            cancellationToken);

        if (isMemberOrFounder)
        {
            throw new ApiException("Project members cannot apply to their own project", HttpStatusCode.Conflict);
        }

        if (!await CanApplyToProjectAsync(projectId, userId, cancellationToken))
        {
            throw new ApiException("You do not have permission to apply to this project", HttpStatusCode.Forbidden);
        }

        var duplicate = await dbContext.ProjectApplications.AnyAsync(
            application => application.ProjectId == projectId && application.ApplicantUserId == userId,
            cancellationToken);

        if (duplicate)
        {
            throw new ApiException("You already applied to this project", HttpStatusCode.Conflict);
        }

        if (request.CvId is not null)
        {
            var cvExists = await dbContext.CVs.AnyAsync(cv => cv.Id == request.CvId && cv.UserId == userId && !cv.IsDeleted, cancellationToken);
            if (!cvExists)
            {
                throw new ApiException("CV not found", HttpStatusCode.NotFound);
            }
        }

        var application = new ProjectApplication
        {
            ProjectId = projectId,
            ApplicantUserId = userId,
            CvId = request.CvId,
            CoverLetter = request.CoverLetter.Trim(),
            Status = ApplicationStatus.Pending
        };

        dbContext.ProjectApplications.Add(application);
        AddHistory(application, ApplicationStatus.Pending, ApplicationStatus.Pending, userId, "Application submitted");
        var notification = await systemSettingReader.GetBooleanAsync("Notifications.Enabled", true, cancellationToken)
            ? AddNotification(
                project.OwnerUserId,
                "New project application",
                $"A new member applied to {project.Title}.",
                application.Id,
                "ProjectApplication",
                $"/projects/{project.Id}/applications/{application.Id}")
            : null;
        AddAudit(userId, "Application.Submit", "ProjectApplication", application.Id, null);
        AddActivity(projectId, userId, ActivityType.ApplicationReceived, ActivityVisibility.MembersOnly, "Application received", "A new member application was received.", "ProjectApplication", application.Id);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_project_applications_ProjectId_ApplicantUserId"
            })
        {
            throw new ApiException("You already applied to this project", HttpStatusCode.Conflict);
        }

        var result = await GetApplicationDtoAsync(application.Id, cancellationToken);
        if (notification is not null)
        {
            await realtimeNotifier.NotificationCreatedAsync(project.OwnerUserId, notification.ToDto(), cancellationToken);
        }
        await realtimeNotifier.ApplicationStatusChangedAsync(userId, projectId, result, cancellationToken);
        return result;
    }

    public async Task<IReadOnlyCollection<ApplicationDto>> GetProjectApplicationsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureCanManageProjectAsync(projectId, userId, cancellationToken);

        return await QueryApplications()
            .Where(application => application.ProjectId == projectId)
            .OrderByDescending(application => application.CreatedAt)
            .Take(200)
            .Select(application => MapApplication(application))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ApplicationDetailDto> GetProjectApplicationAsync(ClaimsPrincipal principal, Guid projectId, Guid applicationId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var application = await QueryApplications()
            .FirstOrDefaultAsync(item => item.Id == applicationId && item.ProjectId == projectId, cancellationToken)
            ?? throw new ApiException("Application not found", HttpStatusCode.NotFound);

        var canView = application.ApplicantUserId == userId ||
            await CanManageProjectAsync(projectId, userId, cancellationToken);

        if (!canView)
        {
            throw new ApiException("You do not have permission to view this application", HttpStatusCode.Forbidden);
        }

        var history = await dbContext.ApplicationStatusHistories
            .Where(item => item.ApplicationId == applicationId)
            .OrderBy(item => item.CreatedAt)
            .Take(500)
            .Select(item => new ApplicationStatusHistoryDto(item.Id, item.FromStatus, item.ToStatus, item.ChangedByUserId, item.Reason, item.CreatedAt))
            .ToArrayAsync(cancellationToken);

        return new ApplicationDetailDto(MapApplication(application), history);
    }

    public async Task<IReadOnlyCollection<ApplicationDto>> GetMyApplicationsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        return await QueryApplications()
            .Where(application => application.ApplicantUserId == userId)
            .OrderByDescending(application => application.CreatedAt)
            .Take(200)
            .Select(application => MapApplication(application))
            .ToArrayAsync(cancellationToken);
    }

    public async Task WithdrawAsync(ClaimsPrincipal principal, Guid projectId, Guid applicationId, ApplicationDecisionRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await using var transaction = await BeginApplicationTransitionAsync(applicationId, cancellationToken);
        var application = await dbContext.ProjectApplications.FirstOrDefaultAsync(
            item => item.Id == applicationId && item.ProjectId == projectId,
            cancellationToken)
            ?? throw new ApiException("Application not found", HttpStatusCode.NotFound);

        if (application.ApplicantUserId != userId)
        {
            throw new ApiException("Only the applicant can withdraw this application", HttpStatusCode.Forbidden);
        }

        if (application.Status is ApplicationStatus.Accepted or ApplicationStatus.Rejected or ApplicationStatus.Withdrawn or ApplicationStatus.Cancelled)
        {
            throw new ApiException("Application cannot be withdrawn from its current status", HttpStatusCode.BadRequest);
        }

        var change = await ChangeStatusAsync(application, ApplicationStatus.Withdrawn, userId, request, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await PublishApplicationChangeAsync(change, cancellationToken);
    }

    public Task<ApplicationDto> ShortlistAsync(ClaimsPrincipal principal, Guid projectId, Guid applicationId, ApplicationDecisionRequest request, CancellationToken cancellationToken)
    {
        return FounderTransitionAsync(principal, projectId, applicationId, ApplicationStatus.Shortlisted, request, cancellationToken);
    }

    public Task<ApplicationDto> InterviewAsync(ClaimsPrincipal principal, Guid projectId, Guid applicationId, ApplicationDecisionRequest request, CancellationToken cancellationToken)
    {
        return FounderTransitionAsync(principal, projectId, applicationId, ApplicationStatus.Interviewing, request, cancellationToken);
    }

    public Task<ApplicationDto> AcceptAsync(ClaimsPrincipal principal, Guid projectId, Guid applicationId, ApplicationDecisionRequest request, CancellationToken cancellationToken)
    {
        return FounderTransitionAsync(principal, projectId, applicationId, ApplicationStatus.Accepted, request, cancellationToken);
    }

    public Task<ApplicationDto> RejectAsync(ClaimsPrincipal principal, Guid projectId, Guid applicationId, ApplicationDecisionRequest request, CancellationToken cancellationToken)
    {
        return FounderTransitionAsync(principal, projectId, applicationId, ApplicationStatus.Rejected, request, cancellationToken);
    }

    private async Task<ApplicationDto> FounderTransitionAsync(
        ClaimsPrincipal principal,
        Guid projectId,
        Guid applicationId,
        ApplicationStatus nextStatus,
        ApplicationDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureCanManageProjectAsync(projectId, userId, cancellationToken);
        await using var transaction = await BeginApplicationTransitionAsync(applicationId, cancellationToken);

        var application = await dbContext.ProjectApplications
            .Include(item => item.Project)
            .FirstOrDefaultAsync(item => item.Id == applicationId && item.ProjectId == projectId, cancellationToken)
            ?? throw new ApiException("Application not found", HttpStatusCode.NotFound);

        ValidateFounderTransition(application.Status, nextStatus);
        var change = await ChangeStatusAsync(application, nextStatus, userId, request, cancellationToken);

        if (nextStatus == ApplicationStatus.Accepted)
        {
            var exists = await dbContext.ProjectMembers.AnyAsync(
                member => member.ProjectId == projectId && member.UserId == change.Application.ApplicantUserId,
                cancellationToken);

            if (!exists)
            {
                dbContext.ProjectMembers.Add(new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = change.Application.ApplicantUserId,
                    Role = ProjectMemberRole.Member
                });
                AddActivity(projectId, change.Application.ApplicantUserId, ActivityType.MemberJoined, ActivityVisibility.MembersOnly, "Member joined", "An accepted applicant joined the project.", "ProjectMember", change.Application.ApplicantUserId);
                AddAudit(userId, "Application.Accept.CreateProjectMember", "Project", projectId, request.Reason);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        await PublishApplicationChangeAsync(change, cancellationToken);
        return change.Application;
    }

    private async Task<ApplicationStatusChangeResult> ChangeStatusAsync(
        ProjectApplication application,
        ApplicationStatus nextStatus,
        Guid changedByUserId,
        ApplicationDecisionRequest request,
        CancellationToken cancellationToken)
    {
        ValidateMaximumLength(request.Reason, 1000, "reason");
        ValidateMaximumLength(request.FounderNote, 1000, "founderNote");
        var previous = application.Status;
        application.Status = nextStatus;
        application.FounderNote = string.IsNullOrWhiteSpace(request.FounderNote) ? application.FounderNote : request.FounderNote.Trim();
        application.UpdatedAt = DateTimeOffset.UtcNow;
        if (nextStatus is ApplicationStatus.Accepted or ApplicationStatus.Rejected or ApplicationStatus.Withdrawn)
        {
            application.DecidedAt = DateTimeOffset.UtcNow;
        }

        AddHistory(application, previous, nextStatus, changedByUserId, request.Reason);
        var notification = await systemSettingReader.GetBooleanAsync("Notifications.Enabled", true, cancellationToken)
            ? AddNotification(
                application.ApplicantUserId,
                "Application status updated",
                $"Your application status is now {nextStatus}.",
                application.Id,
                "ProjectApplication",
                $"/applications/{application.Id}")
            : null;
        AddAudit(changedByUserId, $"Application.Status.{nextStatus}", "ProjectApplication", application.Id, request.Reason);
        if (nextStatus == ApplicationStatus.Accepted)
        {
            AddActivity(application.ProjectId, changedByUserId, ActivityType.ApplicationAccepted, ActivityVisibility.MembersOnly, "Application accepted", "A project application was accepted.", "ProjectApplication", application.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var result = await GetApplicationDtoAsync(application.Id, cancellationToken);
        return new ApplicationStatusChangeResult(result, notification);
    }

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginApplicationTransitionAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var lockResource = $"application-transition:{applicationId:N}";
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({lockResource}, 0))",
                cancellationToken);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    private async Task PublishApplicationChangeAsync(ApplicationStatusChangeResult change, CancellationToken cancellationToken)
    {
        if (change.Notification is not null)
        {
            await realtimeNotifier.NotificationCreatedAsync(change.Application.ApplicantUserId, change.Notification.ToDto(), cancellationToken);
        }

        await realtimeNotifier.ApplicationStatusChangedAsync(
            change.Application.ApplicantUserId,
            change.Application.ProjectId,
            change.Application,
            cancellationToken);
    }

    private sealed record ApplicationStatusChangeResult(ApplicationDto Application, Notification? Notification);

    private static void ValidateFounderTransition(ApplicationStatus currentStatus, ApplicationStatus nextStatus)
    {
        var isValid = nextStatus switch
        {
            ApplicationStatus.Shortlisted => currentStatus == ApplicationStatus.Pending,
            ApplicationStatus.Interviewing => currentStatus is ApplicationStatus.Pending or ApplicationStatus.Shortlisted,
            ApplicationStatus.Accepted => currentStatus is ApplicationStatus.Pending or ApplicationStatus.Shortlisted or ApplicationStatus.Interviewing,
            ApplicationStatus.Rejected => currentStatus is ApplicationStatus.Pending or ApplicationStatus.Shortlisted or ApplicationStatus.Interviewing,
            _ => false
        };

        if (!isValid)
        {
            throw new ApiException("Invalid application status transition", HttpStatusCode.BadRequest);
        }
    }

    private IQueryable<ProjectApplication> QueryApplications()
    {
        return dbContext.ProjectApplications
            .Include(application => application.Project)
            .Include(application => application.ApplicantUser)
            .Include(application => application.Cv);
    }

    private async Task<ApplicationDto> GetApplicationDtoAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await QueryApplications()
            .FirstAsync(item => item.Id == applicationId, cancellationToken);

        return MapApplication(application);
    }

    private static ApplicationDto MapApplication(ProjectApplication application)
    {
        return new ApplicationDto(
            application.Id,
            application.ProjectId,
            application.Project.Title,
            application.ApplicantUserId,
            application.ApplicantUser.Email,
            application.ApplicantUser.FullName,
            application.CvId,
            application.Cv?.Title,
            application.CoverLetter,
            application.Status,
            application.FounderNote,
            application.CreatedAt,
            application.UpdatedAt);
    }

    private async Task EnsureVerifiedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var isVerified = await dbContext.UserRoles
            .Include(userRole => userRole.Role)
            .AnyAsync(userRole => userRole.UserId == userId && userRole.Role.Code == SystemRoles.VerifiedUser, cancellationToken);

        if (!isVerified)
        {
            throw new ApiException("Email verification is required to apply", HttpStatusCode.Forbidden);
        }
    }

    private async Task EnsureCanManageProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await CanManageProjectAsync(projectId, userId, cancellationToken))
        {
            throw new ApiException("You do not have permission to manage project applications", HttpStatusCode.Forbidden);
        }
    }

    private async Task<bool> CanManageProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.ProjectMembers.AnyAsync(
            member => member.ProjectId == projectId &&
                member.UserId == userId &&
                member.IsActive &&
                (member.Role == ProjectMemberRole.Founder || member.Role == ProjectMemberRole.CoFounder),
            cancellationToken);
    }

    private async Task<bool> CanApplyToProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        var setting = await dbContext.ProjectVisibilitySettings
            .Where(item => item.ProjectId == projectId)
            .Select(item => new { item.Visibility, item.RequiresNda })
            .FirstOrDefaultAsync(cancellationToken);

        if (setting is null)
        {
            return false;
        }

        if (setting.Visibility is ProjectVisibility.Public or ProjectVisibility.Limited)
        {
            return true;
        }

        var hasAccessGrant = await dbContext.ProjectAccessGrants.AnyAsync(
            grant => grant.ProjectId == projectId &&
                grant.UserId == userId &&
                (grant.ExpiresAt == null || grant.ExpiresAt > DateTimeOffset.UtcNow),
            cancellationToken);
        if (hasAccessGrant)
        {
            return true;
        }

        if (!setting.RequiresNda)
        {
            return false;
        }

        var activeVersionId = await dbContext.NdaTemplateVersions
            .Where(version => version.IsPublished && version.Template.IsActive)
            .OrderByDescending(version => version.CreatedAt)
            .ThenByDescending(version => version.VersionNumber)
            .Select(version => (Guid?)version.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return activeVersionId.HasValue && await dbContext.NdaAgreements.AnyAsync(
            agreement => agreement.ProjectId == projectId &&
                agreement.UserId == userId &&
                agreement.TemplateVersionId == activeVersionId.Value,
            cancellationToken);
    }

    private void AddHistory(ProjectApplication application, ApplicationStatus fromStatus, ApplicationStatus toStatus, Guid changedByUserId, string? reason)
    {
        dbContext.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            Application = application,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedByUserId = changedByUserId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
        });
    }

    private Notification AddNotification(Guid userId, string title, string message, Guid resourceId, string resourceType, string actionUrl)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = NotificationType.Application,
            Title = title,
            Message = message,
            ResourceId = resourceId,
            ResourceType = resourceType,
            ActionUrl = actionUrl
        };
        dbContext.Notifications.Add(notification);
        return notification;
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

    private void AddActivity(Guid projectId, Guid actorUserId, ActivityType type, ActivityVisibility visibility, string title, string? message, string targetType, Guid targetId)
    {
        dbContext.Activities.Add(new Activity
        {
            ProjectId = projectId,
            ActorUserId = actorUserId,
            Type = type,
            Visibility = visibility,
            Title = title,
            Message = message,
            TargetType = targetType,
            TargetId = targetId
        });
    }

    private static void ValidateCoverLetter(string coverLetter)
    {
        if (string.IsNullOrWhiteSpace(coverLetter))
        {
            throw new ValidationException([new ErrorDetail("Required", "Cover letter is required", "coverLetter")]);
        }

        if (coverLetter.Trim().Length > 3000)
        {
            throw new ValidationException([new ErrorDetail("CoverLetterTooLong", "Cover letter must be at most 3000 characters", "coverLetter")]);
        }
    }

    private static void ValidateMaximumLength(string? value, int maximum, string field)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maximum)
        {
            throw new ValidationException([new ErrorDetail("TooLong", $"{field} must be at most {maximum} characters", field)]);
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
