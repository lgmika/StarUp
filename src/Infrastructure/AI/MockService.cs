using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.AI.Dtos;
using StartupConnect.Application.AI.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;

namespace StartupConnect.Infrastructure.AI;

public sealed class MockAIService(AppDbContext dbContext) : IAIService
{
    private const int DailyQuota = 20;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyCollection<AIRecommendationDto>> CreateProjectSuggestionsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var project = await GetProjectAsync(projectId, cancellationToken);
        await EnsureCanManageProjectAsync(projectId, userId, cancellationToken);
        await EnsureQuotaAsync(userId, cancellationToken);

        var aiRequest = CreateRequest(userId, projectId, AIRequestType.ProjectSuggestions, CreateProjectSnapshot(project));
        dbContext.AIRequests.Add(aiRequest);

        var recommendations = new[]
        {
            new AIRecommendation
            {
                ProjectId = projectId,
                RequestedByUserId = userId,
                AIRequest = aiRequest,
                Title = "Clarify the target user",
                TargetField = "TargetMarket",
                Content = $"Describe exactly who has the painful problem behind '{project.Title}' and why they need this now."
            },
            new AIRecommendation
            {
                ProjectId = projectId,
                RequestedByUserId = userId,
                AIRequest = aiRequest,
                Title = "Strengthen the solution",
                TargetField = "Solution",
                Content = "Add the key workflow, differentiator, and first measurable outcome the MVP will deliver."
            },
            new AIRecommendation
            {
                ProjectId = projectId,
                RequestedByUserId = userId,
                AIRequest = aiRequest,
                Title = "Improve recruiting needs",
                TargetField = "RequiredRoles",
                Content = "List concrete role responsibilities, weekly time expectation, and must-have skills for each needed teammate."
            }
        };

        dbContext.AIRecommendations.AddRange(recommendations);
        AddAudit(userId, "AI.ProjectSuggestions", "Project", projectId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return recommendations.Select(MapRecommendation).ToArray();
    }

    public async Task<AIReviewDto> CreateProjectReviewAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var project = await GetProjectAsync(projectId, cancellationToken);
        await EnsureCanManageProjectAsync(projectId, userId, cancellationToken);
        await EnsureQuotaAsync(userId, cancellationToken);

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(project.TargetMarket)) missing.Add("Target market is missing.");
        if (string.IsNullOrWhiteSpace(project.BusinessModel)) missing.Add("Business model is missing.");
        if (!await dbContext.ProjectRequiredRoles.AnyAsync(role => role.ProjectId == projectId, cancellationToken)) missing.Add("Required team roles are missing.");

        var risks = new List<string>();
        if (project.Summary.Length < 80) risks.Add("Project summary may be too short for reviewers.");
        var visibility = await dbContext.ProjectVisibilitySettings
            .Where(setting => setting.ProjectId == projectId)
            .Select(setting => setting.Visibility)
            .FirstAsync(cancellationToken);
        if (visibility == ProjectVisibility.Public && !string.IsNullOrWhiteSpace(project.PitchDeckUrl)) risks.Add("Public project includes a pitch deck URL; confirm it does not expose sensitive details.");

        var suggestions = new[]
        {
            "Explain the problem with one concrete customer scenario.",
            "Add measurable MVP success criteria.",
            "Describe what kind of teammate or investor would be most helpful now."
        };

        var score = Math.Clamp(85 - missing.Count * 10 - risks.Count * 7, 35, 95);
        var aiRequest = CreateRequest(userId, projectId, AIRequestType.ProjectReview, CreateProjectSnapshot(project));
        var review = new AIReview
        {
            ProjectId = projectId,
            RequestedByUserId = userId,
            AIRequest = aiRequest,
            QualityScore = score,
            MissingInformationJson = JsonSerializer.Serialize(missing, JsonOptions),
            RiskFlagsJson = JsonSerializer.Serialize(risks, JsonOptions),
            SuggestionsJson = JsonSerializer.Serialize(suggestions, JsonOptions),
            Summary = "Mock review only: use this as a checklist, not as an approval decision."
        };

        dbContext.AIRequests.Add(aiRequest);
        dbContext.AIReviews.Add(review);
        AddAudit(userId, "AI.ProjectReview", "Project", projectId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapReview(review);
    }

    public async Task<IReadOnlyCollection<AIReviewDto>> GetProjectReviewsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureCanViewProjectAsync(projectId, userId, cancellationToken);

        var reviews = await dbContext.AIReviews
            .Where(review => review.ProjectId == projectId)
            .OrderByDescending(review => review.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return reviews.Select(MapReview).ToArray();
    }

    public async Task<AIReviewDto> GetLatestProjectReviewAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureCanViewProjectAsync(projectId, userId, cancellationToken);

        var review = await dbContext.AIReviews
            .Where(item => item.ProjectId == projectId)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ApiException("AI review not found", HttpStatusCode.NotFound);

        return MapReview(review);
    }

    public async Task<ApplyAIRecommendationResponse> ApplyRecommendationAsync(ClaimsPrincipal principal, Guid recommendationId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var recommendation = await dbContext.AIRecommendations.FirstOrDefaultAsync(item => item.Id == recommendationId, cancellationToken)
            ?? throw new ApiException("AI recommendation not found", HttpStatusCode.NotFound);

        await EnsureCanManageProjectAsync(recommendation.ProjectId, userId, cancellationToken);

        recommendation.IsApplied = true;
        recommendation.AppliedAt = DateTimeOffset.UtcNow;
        AddAudit(userId, "AI.Recommendation.Apply", "AIRecommendation", recommendation.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ApplyAIRecommendationResponse(recommendation.Id, true, recommendation.AppliedAt.Value);
    }

    public async Task<AITextResponse> CreateCoverLetterAsync(ClaimsPrincipal principal, Guid applicationId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureQuotaAsync(userId, cancellationToken);

        var aiRequest = CreateRequest(userId, null, AIRequestType.ApplicationCoverLetter, $"application:{applicationId}");
        dbContext.AIRequests.Add(aiRequest);
        AddAudit(userId, "AI.CoverLetter", "Application", applicationId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AITextResponse("Mock cover letter: I am excited about this project because it matches my skills and I can contribute consistently to the MVP goals.");
    }

    public async Task<AITextResponse> CreateInvestorSummaryAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var project = await GetProjectAsync(projectId, cancellationToken);
        await EnsureCanViewProjectAsync(projectId, userId, cancellationToken);
        await EnsureQuotaAsync(userId, cancellationToken);

        var aiRequest = CreateRequest(userId, projectId, AIRequestType.InvestorSummary, CreateProjectSnapshot(project));
        dbContext.AIRequests.Add(aiRequest);
        AddAudit(userId, "AI.InvestorSummary", "Project", projectId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AITextResponse($"Mock investor summary: {project.Title} is at {project.Stage} stage, focused on {project.Summary}. Key review areas: market clarity, MVP traction, team gaps, and funding use.");
    }

    private async Task<Project> GetProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await dbContext.Projects.FirstOrDefaultAsync(project => project.Id == projectId && !project.IsDeleted, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);
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
            throw new ApiException("You do not have permission to use AI for this project", HttpStatusCode.Forbidden);
        }
    }

    private async Task EnsureCanViewProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        var canView = await dbContext.ProjectMembers.AnyAsync(member =>
                member.ProjectId == projectId && member.UserId == userId && member.IsActive,
                cancellationToken) ||
            await dbContext.ProjectAccessGrants.AnyAsync(grant =>
                grant.ProjectId == projectId && grant.UserId == userId && (grant.ExpiresAt == null || grant.ExpiresAt > DateTimeOffset.UtcNow),
                cancellationToken) ||
            await dbContext.Projects.AnyAsync(project =>
                project.Id == projectId && project.OwnerUserId == userId && !project.IsDeleted,
                cancellationToken);

        if (!canView)
        {
            throw new ApiException("You do not have permission to view AI data for this project", HttpStatusCode.Forbidden);
        }
    }

    private async Task EnsureQuotaAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = DateTimeOffset.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var used = await dbContext.AIRequests.CountAsync(request =>
            request.UserId == userId &&
            request.CreatedAt >= today &&
            request.CreatedAt < tomorrow,
            cancellationToken);

        if (used >= DailyQuota)
        {
            throw new ApiException("Daily AI quota exceeded", HttpStatusCode.TooManyRequests);
        }
    }

    private static AIRequest CreateRequest(Guid userId, Guid? projectId, AIRequestType type, string promptSnapshot)
    {
        return new AIRequest
        {
            UserId = userId,
            ProjectId = projectId,
            RequestType = type,
            PromptSnapshot = promptSnapshot,
            Provider = "Mock"
        };
    }

    private static string CreateProjectSnapshot(Project project)
    {
        return JsonSerializer.Serialize(new
        {
            project.Id,
            project.Title,
            project.Summary,
            project.Problem,
            project.Solution,
            project.TargetMarket,
            project.BusinessModel,
            project.Stage
        }, JsonOptions);
    }

    private static AIRecommendationDto MapRecommendation(AIRecommendation recommendation)
    {
        return new AIRecommendationDto(
            recommendation.Id,
            recommendation.ProjectId,
            recommendation.Title,
            recommendation.Content,
            recommendation.TargetField,
            recommendation.IsApplied,
            recommendation.CreatedAt);
    }

    private static AIReviewDto MapReview(AIReview review)
    {
        return new AIReviewDto(
            review.Id,
            review.ProjectId,
            review.QualityScore,
            DeserializeStrings(review.MissingInformationJson),
            DeserializeStrings(review.RiskFlagsJson),
            DeserializeStrings(review.SuggestionsJson),
            review.Summary,
            review.CreatedAt);
    }

    private static string[] DeserializeStrings(string json)
    {
        return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
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
