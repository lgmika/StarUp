using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Projects.Dtos;
using StartupConnect.Application.Projects.Interfaces;
using StartupConnect.Application.Realtime.Interfaces;
using StartupConnect.Domain.Constants;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.Projects;

public sealed class ProjectService(
    AppDbContext dbContext,
    IRealtimeNotifier realtimeNotifier) : IProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyCollection<ProjectSummaryDto>> ListProjectsAsync(string? search, CancellationToken cancellationToken)
    {
        var query = BaseProjectQuery()
            .Where(project =>
                project.Status == ProjectStatus.Published &&
                dbContext.ProjectVisibilitySettings.Any(setting =>
                    setting.ProjectId == project.Id &&
                    (setting.Visibility == ProjectVisibility.Public || setting.Visibility == ProjectVisibility.Limited)));

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (search.Trim().Length > 200)
            {
                throw new ValidationException([new ErrorDetail("SearchTooLong", "Search must be at most 200 characters", "search")]);
            }

            var keyword = search.Trim().ToLowerInvariant();
            query = query.Where(project =>
                project.Title.ToLower().Contains(keyword) ||
                project.Summary.ToLower().Contains(keyword));
        }

        var projects = await query
            .OrderByDescending(project => project.CreatedAt)
            .Take(200)
            .ToArrayAsync(cancellationToken);

        return projects.Select(MapSummary).ToArray();
    }

    public async Task<ProjectDetailDto> GetProjectAsync(ClaimsPrincipal? principal, Guid projectId, CancellationToken cancellationToken)
    {
        var project = await GetProjectAggregateAsync(projectId, cancellationToken);
        var userId = TryGetUserId(principal);

        if (!await CanViewProjectAsync(project, userId, cancellationToken))
        {
            throw new ApiException("You do not have permission to view this project", HttpStatusCode.Forbidden);
        }

        return MapDetail(project);
    }

    public async Task RecordProjectViewAsync(
        ClaimsPrincipal? principal,
        Guid projectId,
        Guid visitorId,
        CancellationToken cancellationToken)
    {
        var viewerUserId = TryGetUserId(principal);
        var ownerUserId = await dbContext.Projects
            .Where(project => project.Id == projectId && !project.IsDeleted)
            .Select(project => project.OwnerUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerUserId == Guid.Empty || ownerUserId == viewerUserId)
        {
            return;
        }

        var viewId = Guid.NewGuid();
        var viewedOn = DateOnly.FromDateTime(DateTime.UtcNow);
        var createdAt = DateTimeOffset.UtcNow;

        if (viewerUserId is not null)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO project_views ("Id", "ProjectId", "ViewerUserId", "VisitorId", "ViewedOn", "CreatedAt", "UpdatedAt")
                VALUES ({viewId}, {projectId}, {viewerUserId.Value}, NULL, {viewedOn}, {createdAt}, NULL)
                ON CONFLICT ("ProjectId", "ViewerUserId", "ViewedOn") DO NOTHING
                """, cancellationToken);
            return;
        }

        if (visitorId == Guid.Empty)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO project_views ("Id", "ProjectId", "ViewerUserId", "VisitorId", "ViewedOn", "CreatedAt", "UpdatedAt")
            VALUES ({viewId}, {projectId}, NULL, {visitorId}, {viewedOn}, {createdAt}, NULL)
            ON CONFLICT ("ProjectId", "VisitorId", "ViewedOn") DO NOTHING
            """, cancellationToken);
    }

    public async Task<ProjectDetailDto> CreateDraftAsync(ClaimsPrincipal principal, CreateProjectDraftRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        ValidateDraft(request);

        var project = new Project
        {
            OwnerUserId = userId,
            Title = request.Title.Trim(),
            Slug = await CreateUniqueSlugAsync(request.Title, cancellationToken),
            Summary = request.Summary.Trim(),
            Problem = request.Problem.Trim(),
            Solution = request.Solution.Trim(),
            Stage = request.Stage,
            Status = ProjectStatus.Draft,
            IsRecruiting = true
        };

        dbContext.Projects.Add(project);
        dbContext.ProjectVisibilitySettings.Add(new ProjectVisibilitySetting
        {
            Project = project,
            Visibility = request.Visibility,
            RequiresNda = request.Visibility == ProjectVisibility.NdaRequired
        });
        dbContext.ProjectMembers.Add(new ProjectMember
        {
            Project = project,
            UserId = userId,
            Role = ProjectMemberRole.Founder
        });

        AddAudit(userId, "Project.CreateDraft", "Project", project.Id);
        AddActivity(project, userId, ActivityType.ProjectCreated, ActivityVisibility.Private, "Project draft created", $"Project {project.Title} was created as a draft.", "Project", project.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddVersionAsync(project.Id, userId, "Draft created", cancellationToken);

        return MapDetail(await GetProjectAggregateAsync(project.Id, cancellationToken));
    }

    public async Task<ProjectDetailDto> UpdateProjectAsync(ClaimsPrincipal principal, Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        ValidateUpdate(request);

        var project = await GetProjectAggregateAsync(projectId, cancellationToken);
        await EnsureCanManageProjectAsync(project.Id, userId, cancellationToken);
        var requiresModeration = project.Status == ProjectStatus.Published;

        if (project.Status is ProjectStatus.Published or ProjectStatus.PendingReview)
        {
            await AddVersionAsync(project.Id, userId, "Before project update", cancellationToken);
        }

        project.Title = request.Title.Trim();
        project.Summary = request.Summary.Trim();
        project.Problem = request.Problem.Trim();
        project.Solution = request.Solution.Trim();
        project.TargetMarket = TrimOrNull(request.TargetMarket);
        project.BusinessModel = TrimOrNull(request.BusinessModel);
        project.FundingNeeds = TrimOrNull(request.FundingNeeds);
        project.PitchDeckUrl = TrimOrNull(request.PitchDeckUrl);
        project.Stage = request.Stage;
        project.IsRecruiting = request.IsRecruiting;
        project.UpdatedAt = DateTimeOffset.UtcNow;

        var setting = await dbContext.ProjectVisibilitySettings.FirstAsync(item => item.ProjectId == project.Id, cancellationToken);
        setting.Visibility = request.Visibility;
        setting.RequiresNda = request.Visibility == ProjectVisibility.NdaRequired;
        setting.UpdatedAt = DateTimeOffset.UtcNow;

        if (requiresModeration)
        {
            project.Status = ProjectStatus.PendingReview;
            project.SubmittedAt = DateTimeOffset.UtcNow;
        }

        await ReplaceRequiredRolesAsync(project.Id, request.RequiredRoles, cancellationToken);
        await ReplaceRequiredSkillsAsync(project.Id, request.RequiredSkillIds, cancellationToken);

        AddAudit(userId, requiresModeration ? "Project.Update.SubmitReview" : "Project.Update", "Project", project.Id);
        AddActivity(project, userId, ActivityType.ProjectUpdated, GetProjectUpdateActivityVisibility(project.Status, request.Visibility), "Project updated", $"Project {project.Title} was updated.", "Project", project.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddVersionAsync(project.Id, userId, "Project updated", cancellationToken);
        if (requiresModeration)
        {
            await realtimeNotifier.ProjectStatusChangedAsync(project.Id, MapSummary(project), cancellationToken);
        }

        return MapDetail(await GetProjectAggregateAsync(project.Id, cancellationToken));
    }

    public async Task DeleteProjectAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var project = await dbContext.Projects.FirstOrDefaultAsync(item => item.Id == projectId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);

        await EnsureCanManageProjectAsync(project.Id, userId, cancellationToken);

        project.IsDeleted = true;
        project.Status = ProjectStatus.Archived;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(userId, "Project.Delete", "Project", project.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.ProjectStatusChangedAsync(project.Id, MapSummary(project), cancellationToken);
    }

    public async Task SubmitReviewAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureVerifiedAsync(userId, cancellationToken);

        var project = await dbContext.Projects.FirstOrDefaultAsync(item => item.Id == projectId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);

        await EnsureCanManageProjectAsync(project.Id, userId, cancellationToken);

        if (project.Status is not (ProjectStatus.Draft or ProjectStatus.NeedImprovement))
        {
            throw new ApiException("Only draft or need-improvement projects can be submitted", HttpStatusCode.BadRequest);
        }

        project.Status = ProjectStatus.PendingReview;
        project.SubmittedAt = DateTimeOffset.UtcNow;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(userId, "Project.SubmitReview", "Project", project.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddVersionAsync(project.Id, userId, "Submitted for review", cancellationToken);
        await realtimeNotifier.ProjectStatusChangedAsync(project.Id, MapSummary(project), cancellationToken);
    }

    public async Task CloseProjectAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var project = await dbContext.Projects.FirstOrDefaultAsync(item => item.Id == projectId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);

        await EnsureCanManageProjectAsync(project.Id, userId, cancellationToken);

        project.Status = ProjectStatus.Closed;
        project.IsRecruiting = false;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(userId, "Project.Close", "Project", project.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.ProjectStatusChangedAsync(project.Id, MapSummary(project), cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProjectSummaryDto>> GetOwnedProjectsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var projects = await BaseProjectQuery()
            .Where(project => project.OwnerUserId == userId)
            .OrderByDescending(project => project.CreatedAt)
            .Take(200)
            .ToArrayAsync(cancellationToken);

        return projects.Select(MapSummary).ToArray();
    }

    public async Task<IReadOnlyCollection<ProjectSummaryDto>> GetJoinedProjectsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var projects = await BaseProjectQuery()
            .Where(project => project.Members.Any(member => member.UserId == userId && member.IsActive))
            .OrderByDescending(project => project.CreatedAt)
            .Take(200)
            .ToArrayAsync(cancellationToken);

        return projects.Select(MapSummary).ToArray();
    }

    public async Task<IReadOnlyCollection<ProjectVersionDto>> GetVersionsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureCanManageProjectAsync(projectId, userId, cancellationToken);

        return await dbContext.ProjectVersions
            .Where(version => version.ProjectId == projectId)
            .OrderByDescending(version => version.VersionNumber)
            .Take(200)
            .Select(version => new ProjectVersionDto(version.Id, version.VersionNumber, version.ChangeReason, version.CreatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task SaveProjectAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var project = await dbContext.Projects.FirstOrDefaultAsync(item => item.Id == projectId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);

        if (!await CanViewProjectAsync(project, userId, cancellationToken))
        {
            throw new ApiException("You do not have permission to save this project", HttpStatusCode.Forbidden);
        }

        var exists = await dbContext.SavedProjects.AnyAsync(item => item.UserId == userId && item.ProjectId == projectId, cancellationToken);
        if (!exists)
        {
            dbContext.SavedProjects.Add(new SavedProject { UserId = userId, ProjectId = projectId });
            AddAudit(userId, "Project.Save", "Project", projectId);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UnsaveProjectAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var saved = await dbContext.SavedProjects.FirstOrDefaultAsync(item => item.UserId == userId && item.ProjectId == projectId, cancellationToken);
        if (saved is null)
        {
            return;
        }

        dbContext.SavedProjects.Remove(saved);
        AddAudit(userId, "Project.Unsave", "Project", projectId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProjectSummaryDto>> GetSavedProjectsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var now = DateTimeOffset.UtcNow;
        var projects = await BaseProjectQuery()
            .Where(project =>
                dbContext.SavedProjects.Any(saved => saved.UserId == userId && saved.ProjectId == project.Id) &&
                ((project.Status == ProjectStatus.Published &&
                    dbContext.ProjectVisibilitySettings.Any(setting =>
                        setting.ProjectId == project.Id &&
                        (setting.Visibility == ProjectVisibility.Public || setting.Visibility == ProjectVisibility.Limited))) ||
                 project.OwnerUserId == userId ||
                 dbContext.ProjectMembers.Any(member =>
                     member.ProjectId == project.Id && member.UserId == userId && member.IsActive) ||
                 dbContext.ProjectAccessGrants.Any(grant =>
                     grant.ProjectId == project.Id &&
                     grant.UserId == userId &&
                     (grant.ExpiresAt == null || grant.ExpiresAt > now))))
            .OrderByDescending(project => project.CreatedAt)
            .Take(200)
            .ToArrayAsync(cancellationToken);

        return projects.Select(MapSummary).ToArray();
    }

    private IQueryable<Project> BaseProjectQuery()
    {
        return dbContext.Projects
            .Include(project => project.Members)
            .Where(project => !project.IsDeleted);
    }

    private async Task<Project> GetProjectAggregateAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await dbContext.Projects
            .Include(project => project.Members)
            .FirstOrDefaultAsync(project => project.Id == projectId && !project.IsDeleted, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);
    }

    private async Task<bool> CanViewProjectAsync(Project project, Guid? userId, CancellationToken cancellationToken)
    {
        if (project.Status == ProjectStatus.Published)
        {
            var setting = await dbContext.ProjectVisibilitySettings.FirstAsync(item => item.ProjectId == project.Id, cancellationToken);
            if (setting.Visibility == ProjectVisibility.Public || setting.Visibility == ProjectVisibility.Limited)
            {
                return true;
            }
        }

        if (userId is null)
        {
            return false;
        }

        if (project.OwnerUserId == userId)
        {
            return true;
        }

        var isMember = await dbContext.ProjectMembers.AnyAsync(
            member => member.ProjectId == project.Id && member.UserId == userId && member.IsActive,
            cancellationToken);

        if (isMember)
        {
            return true;
        }

        return await dbContext.ProjectAccessGrants.AnyAsync(
            grant => grant.ProjectId == project.Id &&
                grant.UserId == userId &&
                (grant.ExpiresAt == null || grant.ExpiresAt > DateTimeOffset.UtcNow),
            cancellationToken);
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
            throw new ApiException("You do not have permission to manage this project", HttpStatusCode.Forbidden);
        }
    }

    private async Task EnsureVerifiedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var isVerified = await dbContext.UserRoles
            .Include(userRole => userRole.Role)
            .AnyAsync(userRole => userRole.UserId == userId && userRole.Role.Code == SystemRoles.VerifiedUser, cancellationToken);

        if (!isVerified)
        {
            throw new ApiException("Email verification is required for this action", HttpStatusCode.Forbidden);
        }
    }

    private async Task ReplaceRequiredRolesAsync(Guid projectId, IReadOnlyCollection<UpsertProjectRequiredRoleDto> roles, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ProjectRequiredRoles.Where(role => role.ProjectId == projectId).ToListAsync(cancellationToken);
        dbContext.ProjectRequiredRoles.RemoveRange(existing);

        foreach (var role in roles)
        {
            ValidateRequired(role.RoleName, "roleName", "Required role name is required");
            ValidateMaximumLength(role.RoleName, 120, "roleName");
            ValidateMaximumLength(role.Description, 1000, "description");
            if (role.Slots is < 1 or > 50)
            {
                throw new ValidationException([new ErrorDetail("InvalidSlots", "Role slots must be between 1 and 50", "slots")]);
            }

            dbContext.ProjectRequiredRoles.Add(new ProjectRequiredRole
            {
                ProjectId = projectId,
                RoleName = role.RoleName.Trim(),
                Description = TrimOrNull(role.Description),
                Slots = role.Slots,
                IsOpen = role.IsOpen
            });
        }
    }

    private async Task ReplaceRequiredSkillsAsync(Guid projectId, IReadOnlyCollection<Guid> skillIds, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ProjectRequiredSkills.Where(skill => skill.ProjectId == projectId).ToListAsync(cancellationToken);
        dbContext.ProjectRequiredSkills.RemoveRange(existing);

        var distinctSkillIds = skillIds.Distinct().ToArray();
        var existingSkillCount = await dbContext.Skills.CountAsync(skill => distinctSkillIds.Contains(skill.Id), cancellationToken);
        if (existingSkillCount != distinctSkillIds.Length)
        {
            throw new ApiException("One or more skills were not found", HttpStatusCode.BadRequest);
        }

        foreach (var skillId in distinctSkillIds)
        {
            dbContext.ProjectRequiredSkills.Add(new ProjectRequiredSkill { ProjectId = projectId, SkillId = skillId });
        }
    }

    private async Task AddVersionAsync(Guid projectId, Guid changedByUserId, string reason, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.AsNoTracking().FirstAsync(item => item.Id == projectId, cancellationToken);
        var latestVersion = await dbContext.ProjectVersions
            .Where(version => version.ProjectId == projectId)
            .MaxAsync(version => (int?)version.VersionNumber, cancellationToken) ?? 0;

        dbContext.ProjectVersions.Add(new ProjectVersion
        {
            ProjectId = projectId,
            ChangedByUserId = changedByUserId,
            VersionNumber = latestVersion + 1,
            ChangeReason = reason,
            SnapshotJson = JsonSerializer.Serialize(project, JsonOptions)
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> CreateUniqueSlugAsync(string title, CancellationToken cancellationToken)
    {
        var baseSlug = new string(title.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());

        while (baseSlug.Contains("--", StringComparison.Ordinal))
        {
            baseSlug = baseSlug.Replace("--", "-", StringComparison.Ordinal);
        }

        baseSlug = baseSlug.Trim('-');
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "project";
        }

        var slug = baseSlug;
        var suffix = 1;
        while (await dbContext.Projects.AnyAsync(project => project.Slug == slug, cancellationToken))
        {
            suffix++;
            slug = $"{baseSlug}-{suffix}";
        }

        return slug;
    }

    private ProjectSummaryDto MapSummary(Project project)
    {
        var visibility = dbContext.ProjectVisibilitySettings
            .Where(setting => setting.ProjectId == project.Id)
            .Select(setting => setting.Visibility)
            .FirstOrDefault();

        return new ProjectSummaryDto(
            project.Id,
            project.Title,
            project.Slug,
            project.Summary,
            project.Status,
            project.Stage,
            visibility,
            project.IsRecruiting,
            project.CreatedAt);
    }

    private ProjectDetailDto MapDetail(Project project)
    {
        var setting = dbContext.ProjectVisibilitySettings.First(item => item.ProjectId == project.Id);
        var roles = dbContext.ProjectRequiredRoles
            .Where(role => role.ProjectId == project.Id)
            .OrderBy(role => role.RoleName)
            .Select(role => new ProjectRequiredRoleDto(role.Id, role.RoleName, role.Description, role.Slots, role.IsOpen))
            .ToArray();

        var skills = dbContext.ProjectRequiredSkills
            .Include(requiredSkill => requiredSkill.Skill)
            .Where(requiredSkill => requiredSkill.ProjectId == project.Id)
            .OrderBy(requiredSkill => requiredSkill.Skill.Name)
            .Select(requiredSkill => new ProjectSkillDto(requiredSkill.SkillId, requiredSkill.Skill.Name))
            .ToArray();

        return new ProjectDetailDto(
            project.Id,
            project.OwnerUserId,
            project.Title,
            project.Slug,
            project.Summary,
            project.Problem,
            project.Solution,
            project.TargetMarket,
            project.BusinessModel,
            project.FundingNeeds,
            project.PitchDeckUrl,
            project.Status,
            project.Stage,
            project.IsRecruiting,
            setting.Visibility,
            setting.RequiresNda,
            roles,
            skills,
            project.CreatedAt,
            project.UpdatedAt);
    }

    private void AddAudit(Guid actorUserId, string action, string resourceType, Guid resourceId)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId
        });
    }

    private void AddActivity(Project project, Guid actorUserId, ActivityType type, ActivityVisibility visibility, string title, string? message, string targetType, Guid targetId)
    {
        dbContext.Activities.Add(new Activity
        {
            Project = project,
            ActorUserId = actorUserId,
            Type = type,
            Visibility = visibility,
            Title = title,
            Message = message,
            TargetType = targetType,
            TargetId = targetId
        });
    }

    private static ActivityVisibility GetProjectUpdateActivityVisibility(ProjectStatus status, ProjectVisibility visibility)
    {
        return status == ProjectStatus.Published && visibility is ProjectVisibility.Public or ProjectVisibility.Limited
            ? ActivityVisibility.Public
            : ActivityVisibility.MembersOnly;
    }

    private static void ValidateDraft(CreateProjectDraftRequest request)
    {
        ValidateRequired(request.Title, "title", "Project title is required");
        ValidateRequired(request.Summary, "summary", "Project summary is required");
        ValidateRequired(request.Problem, "problem", "Project problem is required");
        ValidateRequired(request.Solution, "solution", "Project solution is required");
        ValidateProjectText(request.Title, request.Summary, request.Problem, request.Solution);
    }

    private static void ValidateUpdate(UpdateProjectRequest request)
    {
        ValidateRequired(request.Title, "title", "Project title is required");
        ValidateRequired(request.Summary, "summary", "Project summary is required");
        ValidateRequired(request.Problem, "problem", "Project problem is required");
        ValidateRequired(request.Solution, "solution", "Project solution is required");
        ValidateProjectText(request.Title, request.Summary, request.Problem, request.Solution);
        ValidateMaximumLength(request.TargetMarket, 1000, "targetMarket");
        ValidateMaximumLength(request.BusinessModel, 1000, "businessModel");
        ValidateMaximumLength(request.FundingNeeds, 1000, "fundingNeeds");
        ValidateMaximumLength(request.PitchDeckUrl, 500, "pitchDeckUrl");

        if (request.RequiredRoles is null || request.RequiredSkillIds is null)
        {
            throw new ValidationException([new ErrorDetail("Required", "Required roles and skill IDs cannot be null", "requiredRoles")]);
        }

        if (!string.IsNullOrWhiteSpace(request.PitchDeckUrl) &&
            (!Uri.TryCreate(request.PitchDeckUrl.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
        {
            throw new ValidationException([new ErrorDetail("InvalidUrl", "Pitch deck URL must be an absolute HTTP/HTTPS URL", "pitchDeckUrl")]);
        }
    }

    private static void ValidateProjectText(string title, string summary, string problem, string solution)
    {
        ValidateMaximumLength(title, 180, "title");
        ValidateMaximumLength(summary, 1000, "summary");
        ValidateMaximumLength(problem, 3000, "problem");
        ValidateMaximumLength(solution, 3000, "solution");
    }

    private static void ValidateMaximumLength(string? value, int maximum, string field)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maximum)
        {
            throw new ValidationException([new ErrorDetail("TooLong", $"{field} must be at most {maximum} characters", field)]);
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
        return TryGetUserId(principal) ?? throw new ApiException("Invalid access token", HttpStatusCode.Unauthorized);
    }

    private static Guid? TryGetUserId(ClaimsPrincipal? principal)
    {
        var userIdValue =
            principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            principal?.FindFirst("sub")?.Value ??
            principal?.FindFirst("nameid")?.Value;

        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
