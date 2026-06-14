using System.Security.Claims;
using System.Net;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Search.Dtos;
using StartupConnect.Application.Search.Interfaces;
using StartupConnect.Domain.Constants;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;

namespace StartupConnect.Infrastructure.Search;

public sealed class SearchService(AppDbContext dbContext) : ISearchService
{
    private const int MaxPageSize = 50;
    private const int MaxSuggestions = 20;

    public async Task<SearchResultPage<ProjectSearchItemDto>> SearchProjectsAsync(
        ClaimsPrincipal? principal,
        ProjectSearchQuery query,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId(principal);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var keyword = NormalizeKeyword(query.Keyword);

        var projects = ApplyProjectVisibility(dbContext.Projects.Where(project => !project.IsDeleted), userId);

        if (query.Status.HasValue)
        {
            projects = projects.Where(project => project.Status == query.Status.Value);
        }

        if (query.Stage.HasValue)
        {
            projects = projects.Where(project => project.Stage == query.Stage.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.RequiredRole))
        {
            projects = projects.Where(project => dbContext.ProjectRequiredRoles.Any(role =>
                role.ProjectId == project.Id &&
                EF.Functions.ToTsVector("english", role.RoleName + " " + (role.Description ?? string.Empty))
                    .Matches(EF.Functions.PlainToTsQuery("english", query.RequiredRole.Trim()))));
        }

        if (query.RequiredSkillId.HasValue)
        {
            projects = projects.Where(project => dbContext.ProjectRequiredSkills.Any(skill =>
                skill.ProjectId == project.Id && skill.SkillId == query.RequiredSkillId.Value));
        }

        if (query.CreatedFrom.HasValue)
        {
            projects = projects.Where(project => project.CreatedAt >= query.CreatedFrom.Value);
        }

        if (query.CreatedTo.HasValue)
        {
            projects = projects.Where(project => project.CreatedAt <= query.CreatedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var tsQuery = EF.Functions.PlainToTsQuery("english", keyword);
            projects = projects.Where(project =>
                EF.Functions.ToTsVector(
                    "english",
                    project.Title + " " +
                    project.Summary + " " +
                    project.Problem + " " +
                    project.Solution + " " +
                    (project.TargetMarket ?? string.Empty) + " " +
                    (project.BusinessModel ?? string.Empty) + " " +
                    (project.FundingNeeds ?? string.Empty))
                .Matches(tsQuery));
        }

        var ranked = projects.Select(project => new ProjectSearchProjection(
            project,
            !string.IsNullOrWhiteSpace(keyword)
                ? EF.Functions.ToTsVector(
                    "english",
                    project.Title + " " +
                    project.Summary + " " +
                    project.Problem + " " +
                    project.Solution + " " +
                    (project.TargetMarket ?? string.Empty) + " " +
                    (project.BusinessModel ?? string.Empty) + " " +
                    (project.FundingNeeds ?? string.Empty))
                    .Rank(EF.Functions.PlainToTsQuery("english", keyword))
                : 0));

        ranked = (query.Sort ?? "relevance").Trim().ToLowerInvariant() switch
        {
            "oldest" => ranked.OrderBy(item => item.Project.CreatedAt),
            "stage" => ranked.OrderBy(item => item.Project.Stage).ThenByDescending(item => item.Project.CreatedAt),
            "newest" => ranked.OrderByDescending(item => item.Project.CreatedAt),
            _ when !string.IsNullOrWhiteSpace(keyword) => ranked.OrderByDescending(item => item.Rank).ThenByDescending(item => item.Project.CreatedAt),
            _ => ranked.OrderByDescending(item => item.Project.CreatedAt)
        };

        var total = await ranked.CountAsync(cancellationToken);
        var rows = await ranked
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                item.Project.Id,
                item.Project.Title,
                item.Project.Slug,
                item.Project.Summary,
                item.Project.Status,
                item.Project.Stage,
                item.Project.IsRecruiting,
                item.Project.CreatedAt,
                item.Rank,
                Visibility = dbContext.ProjectVisibilitySettings
                    .Where(setting => setting.ProjectId == item.Project.Id)
                    .Select(setting => setting.Visibility)
                    .First()
            })
            .ToArrayAsync(cancellationToken);

        var items = rows
            .Select(row => new ProjectSearchItemDto(row.Id, row.Title, row.Slug, row.Summary, row.Status, row.Stage, row.Visibility, row.IsRecruiting, row.CreatedAt, row.Rank))
            .ToArray();

        return new SearchResultPage<ProjectSearchItemDto>(items, total, page, pageSize);
    }

    public async Task<SearchResultPage<MemberSearchItemDto>> SearchMembersAsync(
        ClaimsPrincipal? principal,
        MemberSearchQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var keyword = NormalizeKeyword(query.Keyword);

        var profiles = dbContext.UserProfiles
            .Include(profile => profile.User)
            .Where(profile =>
                profile.ContactVisibility == ContactVisibility.Public &&
                !profile.User.IsDeleted &&
                !profile.User.IsSuspended &&
                profile.User.Status == UserStatus.Active);

        if (query.VerifiedOnly)
        {
            profiles = profiles.Where(profile => dbContext.UserRoles.Any(role =>
                role.UserId == profile.UserId && role.Role.Code == SystemRoles.VerifiedUser));
        }

        if (query.SkillId.HasValue)
        {
            profiles = profiles.Where(profile => dbContext.UserSkills.Any(skill =>
                skill.UserId == profile.UserId &&
                skill.SkillId == query.SkillId.Value &&
                (!query.MinYearsOfExperience.HasValue || skill.YearsOfExperience >= query.MinYearsOfExperience.Value)));
        }
        else if (query.MinYearsOfExperience.HasValue)
        {
            profiles = profiles.Where(profile => dbContext.UserSkills.Any(skill =>
                skill.UserId == profile.UserId && skill.YearsOfExperience >= query.MinYearsOfExperience.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var locationQuery = EF.Functions.PlainToTsQuery("english", query.Location.Trim());
            profiles = profiles.Where(profile =>
                profile.Location != null &&
                EF.Functions.ToTsVector("english", profile.Location).Matches(locationQuery));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var tsQuery = EF.Functions.PlainToTsQuery("english", keyword);
            profiles = profiles.Where(profile =>
                EF.Functions.ToTsVector(
                    "english",
                    profile.User.FullName + " " +
                    profile.Headline + " " +
                    profile.Bio + " " +
                    (profile.Location ?? string.Empty))
                .Matches(tsQuery));
        }

        var ranked = profiles
            .Select(profile => new MemberSearchProjection(
                profile,
                !string.IsNullOrWhiteSpace(keyword)
                    ? EF.Functions.ToTsVector(
                        "english",
                        profile.User.FullName + " " +
                        profile.Headline + " " +
                        profile.Bio + " " +
                        (profile.Location ?? string.Empty))
                        .Rank(EF.Functions.PlainToTsQuery("english", keyword))
                    : 0))
            .OrderByDescending(item => item.Rank)
            .ThenBy(item => item.Profile.User.FullName);

        var total = await ranked.CountAsync(cancellationToken);
        var rows = await ranked
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                item.Profile.UserId,
                item.Profile.User.FullName,
                item.Profile.Headline,
                item.Profile.Location,
                item.Rank
            })
            .ToArrayAsync(cancellationToken);

        var userIds = rows.Select(row => row.UserId).ToArray();
        var skillsByUser = await dbContext.UserSkills
            .Include(userSkill => userSkill.Skill)
            .Where(userSkill => userIds.Contains(userSkill.UserId))
            .GroupBy(userSkill => userSkill.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                Skills = group.Select(userSkill => userSkill.Skill.Name).OrderBy(name => name).ToArray()
            })
            .ToDictionaryAsync(item => item.UserId, item => item.Skills, cancellationToken);

        var items = rows
            .Select(row => new MemberSearchItemDto(
                row.UserId,
                row.FullName,
                row.Headline,
                row.Location,
                skillsByUser.TryGetValue(row.UserId, out var skills) ? skills : [],
                row.Rank))
            .ToArray();

        return new SearchResultPage<MemberSearchItemDto>(items, total, page, pageSize);
    }

    public async Task<SearchSuggestionsResponse> GetSuggestionsAsync(
        ClaimsPrincipal? principal,
        SearchSuggestionQuery query,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId(principal);
        var keyword = NormalizeKeyword(query.Keyword);
        var limit = Math.Clamp(query.Limit, 1, MaxSuggestions);
        var items = new List<SearchSuggestionDto>();

        var skills = dbContext.Skills.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var tsQuery = EF.Functions.PlainToTsQuery("english", keyword);
            skills = skills.Where(skill => EF.Functions.ToTsVector("english", skill.Name).Matches(tsQuery));
        }

        items.AddRange(await skills
            .OrderBy(skill => skill.Name)
            .Take(limit)
            .Select(skill => new SearchSuggestionDto("Skill", skill.Id, skill.Name, null))
            .ToArrayAsync(cancellationToken));

        var remaining = limit - items.Count;
        if (remaining > 0)
        {
            var projects = ApplyProjectVisibility(dbContext.Projects.Where(project => !project.IsDeleted), userId);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var tsQuery = EF.Functions.PlainToTsQuery("english", keyword);
                projects = projects.Where(project =>
                    EF.Functions.ToTsVector("english", project.Title + " " + project.Summary).Matches(tsQuery));
            }

            items.AddRange(await projects
                .OrderByDescending(project => project.CreatedAt)
                .Take(remaining)
                .Select(project => new SearchSuggestionDto("Project", project.Id, project.Title, project.Summary))
                .ToArrayAsync(cancellationToken));
        }

        return new SearchSuggestionsResponse(items);
    }

    public async Task<SearchResultPage<InvestorSearchItemDto>> SearchInvestorsAsync(
        ClaimsPrincipal principal,
        InvestorSearchQuery query,
        CancellationToken cancellationToken)
    {
        EnsureCanSearchInvestors(principal);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var keyword = NormalizeKeyword(query.Keyword);

        var investors = dbContext.InvestorProfiles
            .Include(profile => profile.User)
            .Where(profile => !profile.User.IsDeleted && !profile.User.IsSuspended && profile.User.Status == UserStatus.Active);

        if (query.MinTicketSize.HasValue)
        {
            investors = investors.Where(profile => profile.MaxTicketSize == null || profile.MaxTicketSize >= query.MinTicketSize.Value);
        }

        if (query.MaxTicketSize.HasValue)
        {
            investors = investors.Where(profile => profile.MinTicketSize == null || profile.MinTicketSize <= query.MaxTicketSize.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var tsQuery = EF.Functions.PlainToTsQuery("english", keyword);
            investors = investors.Where(profile =>
                EF.Functions.ToTsVector(
                    "english",
                    profile.DisplayName + " " +
                    (profile.OrganizationName ?? string.Empty) + " " +
                    (profile.Bio ?? string.Empty) + " " +
                    (profile.InvestmentFocus ?? string.Empty))
                .Matches(tsQuery));
        }

        var ranked = investors
            .Select(profile => new InvestorSearchProjection(
                profile,
                !string.IsNullOrWhiteSpace(keyword)
                    ? EF.Functions.ToTsVector(
                        "english",
                        profile.DisplayName + " " +
                        (profile.OrganizationName ?? string.Empty) + " " +
                        (profile.Bio ?? string.Empty) + " " +
                        (profile.InvestmentFocus ?? string.Empty))
                        .Rank(EF.Functions.PlainToTsQuery("english", keyword))
                    : 0))
            .OrderByDescending(item => item.Rank)
            .ThenBy(item => item.Profile.DisplayName);

        var total = await ranked.CountAsync(cancellationToken);
        var items = await ranked
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new InvestorSearchItemDto(
                item.Profile.UserId,
                item.Profile.DisplayName,
                item.Profile.OrganizationName,
                item.Profile.InvestmentFocus,
                item.Profile.MinTicketSize,
                item.Profile.MaxTicketSize,
                item.Rank))
            .ToArrayAsync(cancellationToken);

        return new SearchResultPage<InvestorSearchItemDto>(items, total, page, pageSize);
    }

    private IQueryable<Project> ApplyProjectVisibility(IQueryable<Project> query, Guid? userId)
    {
        var publicQuery = query.Where(project =>
            project.Status == ProjectStatus.Published &&
            dbContext.ProjectVisibilitySettings.Any(setting =>
                setting.ProjectId == project.Id &&
                (setting.Visibility == ProjectVisibility.Public || setting.Visibility == ProjectVisibility.Limited)));

        if (!userId.HasValue)
        {
            return publicQuery;
        }

        var privateQuery = query.Where(project =>
            project.OwnerUserId == userId.Value ||
            dbContext.ProjectMembers.Any(member => member.ProjectId == project.Id && member.UserId == userId.Value && member.IsActive) ||
            dbContext.ProjectAccessGrants.Any(grant =>
                grant.ProjectId == project.Id &&
                grant.UserId == userId.Value &&
                (grant.ExpiresAt == null || grant.ExpiresAt > DateTimeOffset.UtcNow)));

        return publicQuery.Concat(privateQuery).Distinct();
    }

    private static Guid? TryGetUserId(ClaimsPrincipal? principal)
    {
        var userIdValue =
            principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            principal?.FindFirst("sub")?.Value ??
            principal?.FindFirst("nameid")?.Value;

        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private static string? NormalizeKeyword(string? keyword)
    {
        return string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
    }

    private static void EnsureCanSearchInvestors(ClaimsPrincipal principal)
    {
        if (principal.IsInRole(SystemRoles.Investor) ||
            principal.IsInRole(SystemRoles.Business) ||
            principal.IsInRole(SystemRoles.Admin) ||
            principal.IsInRole(SystemRoles.Moderator))
        {
            return;
        }

        throw new ApiException("Investor search requires an investor, business, moderator, or admin role", HttpStatusCode.Forbidden);
    }

    private sealed record ProjectSearchProjection(Project Project, float Rank);

    private sealed record MemberSearchProjection(UserProfile Profile, float Rank);

    private sealed record InvestorSearchProjection(InvestorProfile Profile, float Rank);
}
