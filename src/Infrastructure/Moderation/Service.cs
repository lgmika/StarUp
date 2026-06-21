using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Moderation.Dtos;
using StartupConnect.Application.Moderation.Interfaces;
using StartupConnect.Application.Admin.Interfaces;
using StartupConnect.Application.Realtime.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Infrastructure.Notifications;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.Moderation;

public sealed class ModeratorService(
    AppDbContext dbContext,
    IRealtimeNotifier realtimeNotifier,
    ISystemSettingReader systemSettingReader) : IModeratorService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ModeratorDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        return new ModeratorDashboardDto(
            await dbContext.Projects.CountAsync(project => project.Status == ProjectStatus.PendingReview && !project.IsDeleted, cancellationToken),
            await dbContext.Projects.CountAsync(project => project.Status == ProjectStatus.Published && !project.IsDeleted, cancellationToken),
            await dbContext.Projects.CountAsync(project => project.Status == ProjectStatus.Rejected && !project.IsDeleted, cancellationToken),
            await dbContext.Projects.CountAsync(project => project.Status == ProjectStatus.Hidden && !project.IsDeleted, cancellationToken),
            await dbContext.Reports.CountAsync(report => report.Status == ReportStatus.Pending, cancellationToken));
    }

    public async Task<IReadOnlyCollection<ModeratorProjectQueueItemDto>> GetPendingProjectsAsync(CancellationToken cancellationToken)
    {
        var projects = await dbContext.Projects
            .Where(project => project.Status == ProjectStatus.PendingReview && !project.IsDeleted)
            .OrderBy(project => project.SubmittedAt ?? project.CreatedAt)
            .Take(200)
            .Select(project => new
            {
                project.Id,
                project.Title,
                project.Summary,
                project.Status,
                project.Stage,
                project.SubmittedAt,
                LatestReview = dbContext.AIReviews
                    .Where(review => review.ProjectId == project.Id)
                    .OrderByDescending(review => review.CreatedAt)
                    .Select(review => new { review.QualityScore, review.RiskFlagsJson })
                    .FirstOrDefault()
            })
            .ToArrayAsync(cancellationToken);

        return projects
            .Select(project => new ModeratorProjectQueueItemDto(
                project.Id,
                project.Title,
                project.Summary,
                project.Status,
                project.Stage,
                project.LatestReview?.QualityScore,
                project.LatestReview is null ? [] : DeserializeStrings(project.LatestReview.RiskFlagsJson),
                project.SubmittedAt))
            .ToArray();
    }

    public async Task<ModeratorProjectDetailDto> GetProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .Include(item => item.OwnerUser)
            .FirstOrDefaultAsync(item => item.Id == projectId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);

        var latestReview = await GetLatestAIReviewAsync(project.Id, cancellationToken);
        var history = await dbContext.ProjectModerationReviews
            .Where(review => review.ProjectId == projectId)
            .OrderByDescending(review => review.CreatedAt)
            .Take(500)
            .Select(review => new ModerationReviewDto(
                review.Id,
                review.Decision,
                review.Reason,
                review.AIQualityScoreSnapshot,
                review.CreatedAt))
            .ToArrayAsync(cancellationToken);

        return new ModeratorProjectDetailDto(
            project.Id,
            project.Title,
            project.Summary,
            project.Problem,
            project.Solution,
            project.Status,
            project.Stage,
            project.OwnerUserId,
            project.OwnerUser.Email,
            latestReview?.QualityScore,
            latestReview is null ? [] : DeserializeStrings(latestReview.RiskFlagsJson),
            history);
    }

    public Task ApproveProjectAsync(ClaimsPrincipal principal, Guid projectId, ModerationDecisionRequest request, CancellationToken cancellationToken)
    {
        return ApplyDecisionAsync(principal, projectId, request, ModerationDecision.Approved, ProjectStatus.Published, "Project approved", cancellationToken);
    }

    public Task RequestImprovementAsync(ClaimsPrincipal principal, Guid projectId, ModerationDecisionRequest request, CancellationToken cancellationToken)
    {
        return ApplyDecisionAsync(principal, projectId, request, ModerationDecision.NeedImprovement, ProjectStatus.NeedImprovement, "Project needs improvement", cancellationToken);
    }

    public Task RejectProjectAsync(ClaimsPrincipal principal, Guid projectId, ModerationDecisionRequest request, CancellationToken cancellationToken)
    {
        return ApplyDecisionAsync(principal, projectId, request, ModerationDecision.Rejected, ProjectStatus.Rejected, "Project rejected", cancellationToken);
    }

    public Task HideProjectAsync(ClaimsPrincipal principal, Guid projectId, ModerationDecisionRequest request, CancellationToken cancellationToken)
    {
        return ApplyDecisionAsync(principal, projectId, request, ModerationDecision.Hidden, ProjectStatus.Hidden, "Project hidden", cancellationToken);
    }

    public Task RestoreProjectAsync(ClaimsPrincipal principal, Guid projectId, ModerationDecisionRequest request, CancellationToken cancellationToken)
    {
        return ApplyDecisionAsync(principal, projectId, request, ModerationDecision.Restored, ProjectStatus.Published, "Project restored", cancellationToken);
    }

    private async Task ApplyDecisionAsync(
        ClaimsPrincipal principal,
        Guid projectId,
        ModerationDecisionRequest request,
        ModerationDecision decision,
        ProjectStatus nextStatus,
        string notificationTitle,
        CancellationToken cancellationToken)
    {
        var moderatorUserId = GetUserId(principal);
        ValidateReason(request.Reason);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var lockResource = $"project-moderation:{projectId:N}";
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockResource}, 0))",
            cancellationToken);

        var project = await dbContext.Projects.FirstOrDefaultAsync(item => item.Id == projectId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);

        ValidateTransition(project.Status, decision);

        var latestReview = await GetLatestAIReviewAsync(project.Id, cancellationToken);
        project.Status = nextStatus;
        project.UpdatedAt = DateTimeOffset.UtcNow;

        dbContext.ProjectModerationReviews.Add(new ProjectModerationReview
        {
            ProjectId = project.Id,
            ModeratorUserId = moderatorUserId,
            Decision = decision,
            Reason = request.Reason.Trim(),
            AIQualityScoreSnapshot = latestReview?.QualityScore,
            AIRiskFlagsSnapshotJson = latestReview?.RiskFlagsJson
        });

        Notification? notification = null;
        if (await systemSettingReader.GetBooleanAsync("Notifications.Enabled", true, cancellationToken))
        {
            notification = new Notification
            {
                UserId = project.OwnerUserId,
                Type = NotificationType.ProjectModeration,
                Title = notificationTitle,
                Message = request.Reason.Trim(),
                ResourceId = project.Id,
                ResourceType = "Project",
                ActionUrl = $"/projects/{project.Id}"
            };
            dbContext.Notifications.Add(notification);
        }

        AddAudit(moderatorUserId, $"Moderator.Project.{decision}", "Project", project.Id, request.Reason);
        if (nextStatus == ProjectStatus.Published)
        {
            var setting = await dbContext.ProjectVisibilitySettings.FirstAsync(item => item.ProjectId == project.Id, cancellationToken);
            dbContext.Activities.Add(new Activity
            {
                ProjectId = project.Id,
                ActorUserId = moderatorUserId,
                Type = ActivityType.ProjectPublished,
                Visibility = setting.Visibility is ProjectVisibility.Public or ProjectVisibility.Limited
                    ? ActivityVisibility.Public
                    : ActivityVisibility.MembersOnly,
                Title = "Project published",
                Message = $"Project {project.Title} is now published.",
                TargetType = "Project",
                TargetId = project.Id
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (notification is not null)
        {
            await realtimeNotifier.NotificationCreatedAsync(project.OwnerUserId, notification.ToDto(), cancellationToken);
        }
        await realtimeNotifier.ProjectStatusChangedAsync(project.Id, new
        {
            project.Id,
            project.Title,
            project.Status,
            project.UpdatedAt,
            Decision = decision,
            Reason = request.Reason.Trim()
        }, cancellationToken);
    }

    private static void ValidateTransition(ProjectStatus currentStatus, ModerationDecision decision)
    {
        var isValid = decision switch
        {
            ModerationDecision.Approved => currentStatus is ProjectStatus.PendingReview or ProjectStatus.NeedImprovement,
            ModerationDecision.NeedImprovement => currentStatus is ProjectStatus.PendingReview,
            ModerationDecision.Rejected => currentStatus is ProjectStatus.PendingReview,
            ModerationDecision.Hidden => currentStatus is ProjectStatus.Published,
            ModerationDecision.Restored => currentStatus is ProjectStatus.Hidden,
            _ => false
        };

        if (!isValid)
        {
            throw new ApiException("Invalid moderation transition for current project status", HttpStatusCode.BadRequest);
        }
    }

    private async Task<AIReview?> GetLatestAIReviewAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await dbContext.AIReviews
            .Where(review => review.ProjectId == projectId)
            .OrderByDescending(review => review.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private void AddAudit(Guid actorUserId, string action, string resourceType, Guid resourceId, string reason)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Reason = reason.Trim()
        });
    }

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException([new ErrorDetail("Required", "Moderation reason is required", "reason")]);
        }

        if (reason.Trim().Length > 1000)
        {
            throw new ValidationException([new ErrorDetail("ReasonTooLong", "Moderation reason must be at most 1000 characters", "reason")]);
        }
    }

    private static string[] DeserializeStrings(string json)
    {
        return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
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
