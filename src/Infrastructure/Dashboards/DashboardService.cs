using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartupConnect.Application.Dashboards;
using StartupConnect.Application.Dashboards.Dtos;
using StartupConnect.Application.Dashboards.Interfaces;
using StartupConnect.Domain.Constants;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;

namespace StartupConnect.Infrastructure.Dashboards;

public sealed class DashboardService(
    AppDbContext dbContext,
    IMemoryCache memoryCache) : IDashboardService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public Task<UserDashboardDto> GetMyDashboardAsync(
        ClaimsPrincipal principal,
        DashboardQuery query,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var range = NormalizeRange(query);
        var cacheKey = $"dashboard:me:{userId:N}:{range.From.UtcTicks}:{range.To.UtcTicks}:{range.TimezoneOffsetMinutes}";

        return memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            var applicationsByStatus = await dbContext.ProjectApplications
                .Where(application => application.ApplicantUserId == userId &&
                    application.CreatedAt >= range.From &&
                    application.CreatedAt <= range.To)
                .GroupBy(application => application.Status)
                .Select(group => new CountByStatusDto(group.Key.ToString(), group.Count()))
                .ToArrayAsync(cancellationToken);

            var upcomingInterviews = await dbContext.InterviewParticipants
                .CountAsync(participant =>
                    participant.UserId == userId &&
                    participant.Interview.StartAt >= DateTimeOffset.UtcNow &&
                    participant.Interview.Status != InterviewStatus.Cancelled &&
                    participant.Interview.Status != InterviewStatus.Completed,
                    cancellationToken);

            var profileCompletion = await CalculateProfileCompletionAsync(userId, cancellationToken);

            return new UserDashboardDto(
                range.From,
                range.To,
                range.TimezoneOffsetMinutes,
                applicationsByStatus.Sum(item => item.Count),
                applicationsByStatus,
                upcomingInterviews,
                await dbContext.ProjectMembers.CountAsync(member => member.UserId == userId && member.IsActive, cancellationToken),
                await dbContext.SavedProjects.CountAsync(saved => saved.UserId == userId, cancellationToken),
                profileCompletion);
        })!;
    }

    public Task<FounderProjectDashboardDto> GetFounderProjectDashboardAsync(
        ClaimsPrincipal principal,
        Guid projectId,
        DashboardQuery query,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var range = NormalizeRange(query);
        var cacheKey = $"dashboard:project:{projectId:N}:{userId:N}:{range.From.UtcTicks}:{range.To.UtcTicks}:{range.TimezoneOffsetMinutes}";

        return memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            await EnsureCanManageProjectAsync(projectId, userId, cancellationToken);

            var project = await dbContext.Projects.FirstAsync(project => project.Id == projectId && !project.IsDeleted, cancellationToken);
            var applicationsByStatus = await dbContext.ProjectApplications
                .Where(application => application.ProjectId == projectId &&
                    application.CreatedAt >= range.From &&
                    application.CreatedAt <= range.To)
                .GroupBy(application => application.Status)
                .Select(group => new CountByStatusDto(group.Key.ToString(), group.Count()))
                .ToArrayAsync(cancellationToken);

            var investorInterestsByStatus = await dbContext.InvestorProjectInterests
                .Where(interest => interest.ProjectId == projectId &&
                    interest.CreatedAt >= range.From &&
                    interest.CreatedAt <= range.To)
                .GroupBy(interest => interest.Status)
                .Select(group => new CountByStatusDto(group.Key.ToString(), group.Count()))
                .ToArrayAsync(cancellationToken);

            var acceptedApplications = applicationsByStatus
                .Where(item => item.Status == ApplicationStatus.Accepted.ToString())
                .Sum(item => item.Count);
            var applications = applicationsByStatus.Sum(item => item.Count);

            var statusHistory = await dbContext.ApplicationStatusHistories
                .Where(history => history.Application.ProjectId == projectId &&
                    history.CreatedAt >= range.From &&
                    history.CreatedAt <= range.To)
                .OrderByDescending(history => history.CreatedAt)
                .Take(30)
                .Select(history => new ProjectStatusHistoryDto(
                    history.Id,
                    history.FromStatus,
                    history.ToStatus,
                    history.ChangedByUserId,
                    history.Reason,
                    history.CreatedAt))
                .ToArrayAsync(cancellationToken);

            return new FounderProjectDashboardDto(
                project.Id,
                project.Title,
                range.From,
                range.To,
                range.TimezoneOffsetMinutes,
                ProjectViews: await dbContext.ProjectViews.CountAsync(view =>
                    view.ProjectId == projectId &&
                    view.CreatedAt >= range.From &&
                    view.CreatedAt <= range.To,
                    cancellationToken),
                SavedCount: await dbContext.SavedProjects.CountAsync(saved => saved.ProjectId == projectId, cancellationToken),
                Applications: applications,
                ApplicationConversionRate: DashboardMetrics.ConversionRate(acceptedApplications, applications),
                TeamSize: await dbContext.ProjectMembers.CountAsync(member => member.ProjectId == projectId && member.IsActive, cancellationToken),
                InvestorInterests: investorInterestsByStatus.Sum(item => item.Count),
                NdaAgreements: await dbContext.NdaAgreements.CountAsync(agreement =>
                    agreement.ProjectId == projectId &&
                    agreement.AcceptedAt >= range.From &&
                    agreement.AcceptedAt <= range.To,
                    cancellationToken),
                ApplicationsByStatus: applicationsByStatus,
                InvestorInterestsByStatus: investorInterestsByStatus,
                ProjectStatusHistory: statusHistory);
        })!;
    }

    public Task<InvestorDashboardDto> GetInvestorDashboardAsync(
        ClaimsPrincipal principal,
        DashboardQuery query,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var range = NormalizeRange(query);
        var cacheKey = $"dashboard:investor:{userId:N}:{range.From.UtcTicks}:{range.To.UtcTicks}:{range.TimezoneOffsetMinutes}";

        return memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            await EnsureInvestorRoleAsync(userId, cancellationToken);

            var interestStatus = await dbContext.InvestorProjectInterests
                .Where(interest => interest.InvestorUserId == userId &&
                    interest.CreatedAt >= range.From &&
                    interest.CreatedAt <= range.To)
                .GroupBy(interest => interest.Status)
                .Select(group => new CountByStatusDto(group.Key.ToString(), group.Count()))
                .ToArrayAsync(cancellationToken);

            return new InvestorDashboardDto(
                range.From,
                range.To,
                range.TimezoneOffsetMinutes,
                InterestedProjects: interestStatus.Sum(item => item.Count),
                InterestStatus: interestStatus,
                NdaPending: await dbContext.InvestorProjectInterests.CountAsync(interest =>
                    interest.InvestorUserId == userId &&
                    interest.Status == InvestorInterestStatus.AcceptedPendingNda,
                    cancellationToken),
                AcceptedAccess: await dbContext.ProjectAccessGrants.CountAsync(grant =>
                    grant.UserId == userId &&
                    (grant.ExpiresAt == null || grant.ExpiresAt > DateTimeOffset.UtcNow),
                    cancellationToken),
                SavedProjects: await dbContext.SavedProjects.CountAsync(saved => saved.UserId == userId, cancellationToken));
        })!;
    }

    private async Task<int> CalculateProfileCompletionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        var hasSkill = await dbContext.UserSkills.AnyAsync(skill => skill.UserId == userId, cancellationToken);
        var hasCv = await dbContext.CVs.AnyAsync(cv => cv.UserId == userId && !cv.IsDeleted, cancellationToken);
        var hasPortfolio = await dbContext.Portfolios.AnyAsync(portfolio => portfolio.UserId == userId && !portfolio.IsDeleted, cancellationToken);

        var completed = 0;
        completed += !string.IsNullOrWhiteSpace(profile?.Headline) ? 1 : 0;
        completed += !string.IsNullOrWhiteSpace(profile?.Bio) ? 1 : 0;
        completed += !string.IsNullOrWhiteSpace(profile?.Location) ? 1 : 0;
        completed += hasSkill ? 1 : 0;
        completed += hasCv ? 1 : 0;
        completed += hasPortfolio ? 1 : 0;

        return DashboardMetrics.CompletionPercent(completed, 6);
    }

    private async Task EnsureCanManageProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        var canManage = await dbContext.ProjectMembers.AnyAsync(member =>
            member.ProjectId == projectId &&
            member.UserId == userId &&
            member.IsActive &&
            (member.Role == ProjectMemberRole.Founder || member.Role == ProjectMemberRole.CoFounder),
            cancellationToken);

        if (!canManage)
        {
            throw new ApiException("You do not have permission to view this project dashboard", HttpStatusCode.Forbidden);
        }
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

    private static (DateTimeOffset From, DateTimeOffset To, int TimezoneOffsetMinutes) NormalizeRange(DashboardQuery query)
    {
        var offset = TimeSpan.FromMinutes(Math.Clamp(query.TimezoneOffsetMinutes, -14 * 60, 14 * 60));
        var nowLocal = DateTimeOffset.UtcNow.ToOffset(offset);
        var from = query.From?.ToUniversalTime() ?? nowLocal.AddDays(-30).ToUniversalTime();
        var to = query.To?.ToUniversalTime() ?? nowLocal.ToUniversalTime();

        if (from > to)
        {
            (from, to) = (to, from);
        }

        return (from, to, (int)offset.TotalMinutes);
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
