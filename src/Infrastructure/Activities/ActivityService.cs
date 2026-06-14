using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Activities.Dtos;
using StartupConnect.Application.Activities.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;

namespace StartupConnect.Infrastructure.Activities;

public sealed class ActivityService(AppDbContext dbContext) : IActivityService
{
    private const int MaxPageSize = 100;

    public Task<ActivityListResponse> GetFeedAsync(ClaimsPrincipal? principal, ActivityQuery query, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId(principal);
        return QueryVisibleAsync(userId, null, query, cancellationToken);
    }

    public async Task<ActivityListResponse> GetProjectActivitiesAsync(ClaimsPrincipal? principal, Guid projectId, ActivityQuery query, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId(principal);
        var project = await dbContext.Projects.FirstOrDefaultAsync(item => item.Id == projectId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);

        if (!await CanViewProjectAsync(project, userId, cancellationToken))
        {
            throw new ApiException("You do not have permission to view project activities", HttpStatusCode.Forbidden);
        }

        return await QueryVisibleAsync(userId, projectId, query, cancellationToken);
    }

    private async Task<ActivityListResponse> QueryVisibleAsync(Guid? userId, Guid? projectId, ActivityQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var visible = ApplyVisibility(BaseQuery(), userId);

        if (projectId.HasValue)
        {
            visible = visible.Where(activity => activity.ProjectId == projectId.Value);
        }

        var total = await visible.CountAsync(cancellationToken);
        var items = await visible
            .OrderByDescending(activity => activity.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(activity => Map(activity))
            .ToArrayAsync(cancellationToken);

        return new ActivityListResponse(items, total, page, pageSize);
    }

    private IQueryable<Activity> BaseQuery()
    {
        return dbContext.Activities
            .Include(activity => activity.Project)
            .Include(activity => activity.ActorUser)
            .Where(activity => !activity.Project.IsDeleted);
    }

    private IQueryable<Activity> ApplyVisibility(IQueryable<Activity> query, Guid? userId)
    {
        var publicQuery = query.Where(activity =>
            activity.Visibility == ActivityVisibility.Public &&
            activity.Project.Status == ProjectStatus.Published &&
            dbContext.ProjectVisibilitySettings.Any(setting =>
                setting.ProjectId == activity.ProjectId &&
                (setting.Visibility == ProjectVisibility.Public || setting.Visibility == ProjectVisibility.Limited)));

        if (!userId.HasValue)
        {
            return publicQuery;
        }

        var privateQuery = query.Where(activity =>
            activity.Project.OwnerUserId == userId.Value ||
            dbContext.ProjectMembers.Any(member =>
                member.ProjectId == activity.ProjectId &&
                member.UserId == userId.Value &&
                member.IsActive &&
                (activity.Visibility == ActivityVisibility.MembersOnly ||
                    member.Role == ProjectMemberRole.Founder ||
                    member.Role == ProjectMemberRole.CoFounder)) ||
            dbContext.ProjectAccessGrants.Any(grant =>
                activity.Visibility == ActivityVisibility.MembersOnly &&
                grant.ProjectId == activity.ProjectId &&
                grant.UserId == userId.Value &&
                (grant.ExpiresAt == null || grant.ExpiresAt > DateTimeOffset.UtcNow)));

        return publicQuery.Concat(privateQuery).Distinct();
    }

    private async Task<bool> CanViewProjectAsync(Project project, Guid? userId, CancellationToken cancellationToken)
    {
        if (project.Status == ProjectStatus.Published)
        {
            var setting = await dbContext.ProjectVisibilitySettings.FirstAsync(item => item.ProjectId == project.Id, cancellationToken);
            if (setting.Visibility is ProjectVisibility.Public or ProjectVisibility.Limited)
            {
                return true;
            }
        }

        if (!userId.HasValue)
        {
            return false;
        }

        if (project.OwnerUserId == userId.Value)
        {
            return true;
        }

        return await dbContext.ProjectMembers.AnyAsync(member =>
                member.ProjectId == project.Id && member.UserId == userId.Value && member.IsActive,
                cancellationToken) ||
            await dbContext.ProjectAccessGrants.AnyAsync(grant =>
                grant.ProjectId == project.Id &&
                grant.UserId == userId.Value &&
                (grant.ExpiresAt == null || grant.ExpiresAt > DateTimeOffset.UtcNow),
                cancellationToken);
    }

    private static ActivityDto Map(Activity activity)
    {
        return new ActivityDto(
            activity.Id,
            activity.ProjectId,
            activity.Project.Title,
            activity.ActorUserId,
            activity.ActorUser?.FullName,
            activity.Type,
            activity.Visibility,
            activity.Title,
            activity.Message,
            activity.TargetType,
            activity.TargetId,
            activity.CreatedAt);
    }

    private static Guid? TryGetUserId(ClaimsPrincipal? principal)
    {
        var userIdValue =
            principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            principal?.FindFirst("sub")?.Value ??
            principal?.FindFirst("nameid")?.Value;

        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
