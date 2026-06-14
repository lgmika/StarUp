using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Recommendations;
using StartupConnect.Application.Recommendations.Dtos;
using StartupConnect.Application.Recommendations.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;

namespace StartupConnect.Infrastructure.Recommendations;

public sealed class RecommendationService(AppDbContext dbContext) : IRecommendationService
{
    private const int MaxPageSize = 50;

    public async Task<RecommendationListResponse<ProjectRecommendationDto>> GetProjectRecommendationsAsync(
        ClaimsPrincipal principal,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var normalized = NormalizePage(page, pageSize);
        var dismissedIds = await GetDismissedIdsAsync(userId, RecommendationType.Project, cancellationToken);
        var userSkillIds = await dbContext.UserSkills.Where(skill => skill.UserId == userId).Select(skill => skill.SkillId).ToArrayAsync(cancellationToken);
        var appliedProjectIds = await dbContext.ProjectApplications.Where(application => application.ApplicantUserId == userId).Select(application => application.ProjectId).ToArrayAsync(cancellationToken);
        var savedProjectIds = await dbContext.SavedProjects.Where(saved => saved.UserId == userId).Select(saved => saved.ProjectId).ToArrayAsync(cancellationToken);

        var projects = await VisibleProjects(userId)
            .Where(project => project.IsRecruiting && !appliedProjectIds.Contains(project.Id))
            .OrderByDescending(project => project.CreatedAt)
            .Take(200)
            .Select(project => new
            {
                project.Id,
                project.Title,
                project.Summary,
                project.Stage,
                project.CreatedAt,
                Visibility = dbContext.ProjectVisibilitySettings.Where(setting => setting.ProjectId == project.Id).Select(setting => setting.Visibility).First(),
                RequiredSkillIds = dbContext.ProjectRequiredSkills.Where(skill => skill.ProjectId == project.Id).Select(skill => skill.SkillId).ToArray(),
                HasOpenRole = dbContext.ProjectRequiredRoles.Any(role => role.ProjectId == project.Id && role.IsOpen)
            })
            .ToArrayAsync(cancellationToken);

        var items = projects
            .Select(project =>
            {
                var recommendationId = CreateRecommendationId(RecommendationType.Project, userId, project.Id, null);
                var matched = project.RequiredSkillIds.Intersect(userSkillIds).Count();
                var breakdown = new List<RecommendationBreakdownItemDto>();
                var skillPoints = RecommendationScoring.SkillMatchScore(matched, project.RequiredSkillIds.Length);
                if (skillPoints > 0)
                {
                    breakdown.Add(new("skillMatch", skillPoints, $"{matched}/{project.RequiredSkillIds.Length} required skills match your profile."));
                }

                if (project.HasOpenRole)
                {
                    breakdown.Add(new("openRole", RecommendationScoring.OpenRolePoints, "Project has open roles."));
                }

                if (savedProjectIds.Length > 0)
                {
                    breakdown.Add(new("savedBehavior", RecommendationScoring.SavedBehaviorPoints, "Recommendation considers your saved-project behavior without creating AI requests."));
                }

                var score = RecommendationScoring.ClampScore(breakdown.Sum(item => item.Points));
                return new ProjectRecommendationDto(recommendationId, project.Id, project.Title, project.Summary, project.Stage, project.Visibility, score, breakdown);
            })
            .Where(item => item.Score > 0 && !dismissedIds.Contains(item.RecommendationId))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Title)
            .ToArray();

        return Page(items, normalized.Page, normalized.PageSize);
    }

    public async Task<RecommendationListResponse<MemberRecommendationDto>> GetMemberRecommendationsAsync(
        ClaimsPrincipal principal,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var projectIds = await dbContext.ProjectMembers
            .Where(member => member.UserId == userId && member.IsActive && (member.Role == ProjectMemberRole.Founder || member.Role == ProjectMemberRole.CoFounder))
            .Select(member => member.ProjectId)
            .ToArrayAsync(cancellationToken);

        var items = new List<MemberRecommendationDto>();
        foreach (var projectId in projectIds)
        {
            var result = await BuildMemberRecommendationsAsync(userId, projectId, 1, MaxPageSize, cancellationToken);
            items.AddRange(result.Items);
        }

        var normalized = NormalizePage(page, pageSize);
        var ordered = items.OrderByDescending(item => item.Score).ThenBy(item => item.FullName).ToArray();
        return Page(ordered, normalized.Page, normalized.PageSize);
    }

    public async Task<RecommendationListResponse<MemberRecommendationDto>> GetRecommendedMembersForProjectAsync(
        ClaimsPrincipal principal,
        Guid projectId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureCanManageProjectAsync(projectId, userId, cancellationToken);
        var normalized = NormalizePage(page, pageSize);
        return await BuildMemberRecommendationsAsync(userId, projectId, normalized.Page, normalized.PageSize, cancellationToken);
    }

    public async Task DismissAsync(ClaimsPrincipal principal, Guid recommendationId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var exists = await dbContext.RecommendationDismissals.AnyAsync(
            dismissal => dismissal.UserId == userId && dismissal.RecommendationId == recommendationId,
            cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.RecommendationDismissals.Add(new RecommendationDismissal
        {
            UserId = userId,
            RecommendationId = recommendationId,
            Type = RecommendationType.Project
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<RecommendationListResponse<MemberRecommendationDto>> BuildMemberRecommendationsAsync(
        Guid actorUserId,
        Guid projectId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var dismissedIds = await GetDismissedIdsAsync(actorUserId, RecommendationType.Member, cancellationToken);
        var project = await dbContext.Projects.FirstOrDefaultAsync(item => item.Id == projectId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);
        var requiredSkillIds = await dbContext.ProjectRequiredSkills.Where(skill => skill.ProjectId == projectId).Select(skill => skill.SkillId).ToArrayAsync(cancellationToken);
        var memberUserIds = await dbContext.ProjectMembers.Where(member => member.ProjectId == projectId && member.IsActive).Select(member => member.UserId).ToArrayAsync(cancellationToken);
        var hasOpenRole = await dbContext.ProjectRequiredRoles.AnyAsync(role => role.ProjectId == projectId && role.IsOpen, cancellationToken);

        var candidates = await dbContext.UserProfiles
            .Include(profile => profile.User)
            .Where(profile => profile.ContactVisibility == ContactVisibility.Public &&
                !profile.User.IsDeleted &&
                !profile.User.IsSuspended &&
                profile.User.Status == UserStatus.Active &&
                !memberUserIds.Contains(profile.UserId))
            .Take(300)
            .Select(profile => new
            {
                profile.UserId,
                profile.User.FullName,
                profile.Headline,
                profile.Location,
                profile.Bio,
                profile.LinkedInUrl,
                profile.GitHubUrl,
                profile.WebsiteUrl,
                Skills = dbContext.UserSkills
                    .Where(skill => skill.UserId == profile.UserId)
                    .Select(skill => new { skill.SkillId, skill.Skill.Name, skill.YearsOfExperience })
                    .ToArray(),
                JoinedProjects = dbContext.ProjectMembers.Count(member => member.UserId == profile.UserId && member.IsActive)
            })
            .ToArrayAsync(cancellationToken);

        var items = candidates
            .Select(candidate =>
            {
                var recommendationId = CreateRecommendationId(RecommendationType.Member, actorUserId, projectId, candidate.UserId);
                var matchedSkills = candidate.Skills.Where(skill => requiredSkillIds.Contains(skill.SkillId)).ToArray();
                var breakdown = new List<RecommendationBreakdownItemDto>();
                var skillPoints = RecommendationScoring.SkillMatchScore(matchedSkills.Length, requiredSkillIds.Length);
                if (skillPoints > 0)
                {
                    breakdown.Add(new("skillMatch", skillPoints, $"{matchedSkills.Length}/{requiredSkillIds.Length} required skills match this project."));
                }

                if (matchedSkills.Any(skill => skill.YearsOfExperience >= 2))
                {
                    breakdown.Add(new("experience", RecommendationScoring.ExperiencePoints, "Candidate has experience on at least one matched skill."));
                }

                if (hasOpenRole)
                {
                    breakdown.Add(new("openRole", RecommendationScoring.OpenRolePoints, "Project has open roles."));
                }

                var completeness = ProfileCompleteness(candidate.Headline, candidate.Bio, candidate.Location, candidate.LinkedInUrl, candidate.GitHubUrl, candidate.WebsiteUrl);
                if (completeness >= 4)
                {
                    breakdown.Add(new("profileCompleteness", RecommendationScoring.ProfileCompletenessPoints, "Candidate has a complete public profile."));
                }

                if (candidate.JoinedProjects > 0)
                {
                    breakdown.Add(new("projectHistory", RecommendationScoring.HistoryPoints, "Candidate has previous project membership history."));
                }

                var score = RecommendationScoring.ClampScore(breakdown.Sum(item => item.Points));
                return new MemberRecommendationDto(
                    recommendationId,
                    project.Id,
                    project.Title,
                    candidate.UserId,
                    candidate.FullName,
                    candidate.Headline,
                    candidate.Location,
                    matchedSkills.Select(skill => skill.Name).OrderBy(name => name).ToArray(),
                    score,
                    breakdown);
            })
            .Where(item => item.Score > 0 && !dismissedIds.Contains(item.RecommendationId))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.FullName)
            .ToArray();

        return Page(items, page, pageSize);
    }

    private IQueryable<Project> VisibleProjects(Guid userId)
    {
        return dbContext.Projects.Where(project =>
            !project.IsDeleted &&
            project.Status == ProjectStatus.Published &&
            (dbContext.ProjectVisibilitySettings.Any(setting =>
                    setting.ProjectId == project.Id &&
                    (setting.Visibility == ProjectVisibility.Public || setting.Visibility == ProjectVisibility.Limited)) ||
                project.OwnerUserId == userId ||
                dbContext.ProjectMembers.Any(member => member.ProjectId == project.Id && member.UserId == userId && member.IsActive) ||
                dbContext.ProjectAccessGrants.Any(grant =>
                    grant.ProjectId == project.Id &&
                    grant.UserId == userId &&
                    (grant.ExpiresAt == null || grant.ExpiresAt > DateTimeOffset.UtcNow))));
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
            throw new ApiException("You do not have permission to view member recommendations for this project", HttpStatusCode.Forbidden);
        }
    }

    private async Task<HashSet<Guid>> GetDismissedIdsAsync(Guid userId, RecommendationType type, CancellationToken cancellationToken)
    {
        var ids = await dbContext.RecommendationDismissals
            .Where(dismissal => dismissal.UserId == userId)
            .Select(dismissal => dismissal.RecommendationId)
            .ToArrayAsync(cancellationToken);

        return ids.ToHashSet();
    }

    private static RecommendationListResponse<T> Page<T>(IReadOnlyCollection<T> items, int page, int pageSize)
    {
        var total = items.Count;
        var pageItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        return new RecommendationListResponse<T>(pageItems, total, page, pageSize);
    }

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize)
    {
        return (Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));
    }

    private static int ProfileCompleteness(params string?[] fields)
    {
        return fields.Count(field => !string.IsNullOrWhiteSpace(field));
    }

    private static Guid CreateRecommendationId(RecommendationType type, Guid userId, Guid targetId, Guid? secondaryTargetId)
    {
        var input = $"{type}:{userId:N}:{targetId:N}:{secondaryTargetId:N}";
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(bytes);
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
