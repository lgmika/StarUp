using System.Net;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StartupConnect.Application.AI.Dtos;
using StartupConnect.Application.AI.Interfaces;
using StartupConnect.Application.Admin.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;

namespace StartupConnect.Infrastructure.AI;

public sealed class AIService(
    AppDbContext dbContext,
    IAIProvider aiProvider,
    IOptions<AIOptions> aiOptions,
    ISystemSettingReader systemSettingReader,
    ILogger<AIService> logger) : IAIService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AIOptions _aiOptions = aiOptions.Value;

    public async Task<IReadOnlyCollection<AIRecommendationDto>> CreateProjectSuggestionsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var project = await GetProjectAsync(projectId, cancellationToken);
        await EnsureCanManageProjectAsync(projectId, userId, cancellationToken);
        var projectContext = await CreateProjectContextAsync(project, cancellationToken);

        var aiRequest = CreateRequest(userId, projectId, AIRequestType.ProjectSuggestions, CreateProjectSnapshot(projectContext));
        await ReserveRequestAsync(aiRequest, cancellationToken);

        var suggestionResults = await ExecuteProviderAsync(
            () => aiProvider.GenerateProjectSuggestionsAsync(projectContext, cancellationToken),
            cancellationToken);
        aiRequest.ResponseSnapshot = JsonSerializer.Serialize(suggestionResults, JsonOptions);
        aiRequest.IsSuccessful = true;
        var recommendations = suggestionResults
            .Select(suggestion => new AIRecommendation
            {
                ProjectId = projectId,
                RequestedByUserId = userId,
                AIRequest = aiRequest,
                Title = suggestion.Title,
                TargetField = suggestion.TargetField,
                Content = suggestion.Content
            })
            .ToArray();

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
        var projectContext = await CreateProjectContextAsync(project, cancellationToken);
        var aiRequest = CreateRequest(
            userId,
            projectId,
            AIRequestType.ProjectReview,
            CreateProjectSnapshot(projectContext));
        await ReserveRequestAsync(aiRequest, cancellationToken);
        var result = await ExecuteProviderAsync(
            () => aiProvider.ReviewProjectAsync(projectContext, cancellationToken),
            cancellationToken);
        aiRequest.ResponseSnapshot = JsonSerializer.Serialize(result, JsonOptions);
        aiRequest.IsSuccessful = true;
        var review = new AIReview
        {
            ProjectId = projectId,
            RequestedByUserId = userId,
            AIRequest = aiRequest,
            QualityScore = Math.Clamp(result.QualityScore, 0, 100),
            MissingInformationJson = JsonSerializer.Serialize(result.MissingInformation, JsonOptions),
            RiskFlagsJson = JsonSerializer.Serialize(result.RiskFlags, JsonOptions),
            SuggestionsJson = JsonSerializer.Serialize(result.Suggestions, JsonOptions),
            Summary = result.Summary
        };

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
        var applicationSnapshot = await CreateApplicationSnapshotAsync(applicationId, userId, cancellationToken);
        var aiRequest = CreateRequest(userId, null, AIRequestType.ApplicationCoverLetter, applicationSnapshot);
        await ReserveRequestAsync(aiRequest, cancellationToken);
        var result = await ExecuteProviderAsync(
            () => aiProvider.GenerateCoverLetterAsync(applicationSnapshot, cancellationToken),
            cancellationToken);
        aiRequest.ResponseSnapshot = result.Content;
        aiRequest.IsSuccessful = true;
        AddAudit(userId, "AI.CoverLetter", "Application", applicationId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AITextResponse(result.Content);
    }

    public async Task<AITextResponse> CreateInvestorSummaryAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var project = await GetProjectAsync(projectId, cancellationToken);
        await EnsureCanViewProjectAsync(projectId, userId, cancellationToken);
        var projectContext = await CreateProjectContextAsync(project, cancellationToken);
        var aiRequest = CreateRequest(
            userId,
            projectId,
            AIRequestType.InvestorSummary,
            CreateProjectSnapshot(projectContext));
        await ReserveRequestAsync(aiRequest, cancellationToken);
        var result = await ExecuteProviderAsync(
            () => aiProvider.GenerateInvestorSummaryAsync(projectContext, cancellationToken),
            cancellationToken);
        aiRequest.ResponseSnapshot = result.Content;
        aiRequest.IsSuccessful = true;
        AddAudit(userId, "AI.InvestorSummary", "Project", projectId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AITextResponse(result.Content);
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
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var tomorrow = today.AddDays(1);
        var used = await dbContext.AIRequests.CountAsync(request =>
            request.UserId == userId &&
            request.CreatedAt >= today &&
            request.CreatedAt < tomorrow,
            cancellationToken);

        var dailyQuota = await systemSettingReader.GetInt64Async("AI.DailyQuota", _aiOptions.DailyQuota, cancellationToken);
        if (dailyQuota is < 1 or > 10_000)
        {
            throw new InvalidOperationException("AI.DailyQuota must be between 1 and 10000.");
        }

        if (used >= dailyQuota)
        {
            throw new ApiException("Daily AI quota exceeded", HttpStatusCode.TooManyRequests);
        }
    }

    private async Task ReserveRequestAsync(AIRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var lockBytes = SHA256.HashData(Encoding.UTF8.GetBytes($"ai-quota:{request.UserId:D}:{DateTimeOffset.UtcNow:yyyy-MM-dd}"));
        var lockKey = BinaryPrimitives.ReadInt64BigEndian(lockBytes);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({lockKey})",
            cancellationToken);

        await EnsureQuotaAsync(request.UserId, cancellationToken);
        dbContext.AIRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<T> ExecuteProviderAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "AI provider {Provider} timed out.", aiProvider.Name);
            throw new ApiException("AI provider timed out", HttpStatusCode.GatewayTimeout);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "AI provider {Provider} is unavailable.", aiProvider.Name);
            throw new ApiException("AI provider is temporarily unavailable", HttpStatusCode.ServiceUnavailable);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            logger.LogWarning(exception, "AI provider {Provider} returned an invalid response.", aiProvider.Name);
            throw new ApiException("AI provider returned an invalid response", HttpStatusCode.BadGateway);
        }
    }

    private AIRequest CreateRequest(
        Guid userId,
        Guid? projectId,
        AIRequestType type,
        string promptSnapshot,
        string? responseSnapshot = null)
    {
        return new AIRequest
        {
            UserId = userId,
            ProjectId = projectId,
            RequestType = type,
            PromptSnapshot = promptSnapshot,
            ResponseSnapshot = responseSnapshot,
            Provider = aiProvider.Name,
            IsSuccessful = responseSnapshot is not null
        };
    }

    private async Task<AIProjectContext> CreateProjectContextAsync(Project project, CancellationToken cancellationToken)
    {
        var roles = await dbContext.ProjectRequiredRoles
            .Where(role => role.ProjectId == project.Id)
            .OrderBy(role => role.RoleName)
            .Select(role => role.RoleName)
            .ToArrayAsync(cancellationToken);

        return new AIProjectContext(
            project.Id,
            project.Title,
            project.Summary,
            project.Problem,
            project.Solution,
            project.TargetMarket ?? string.Empty,
            project.BusinessModel ?? string.Empty,
            project.Stage.ToString(),
            roles);
    }

    private async Task<string> CreateApplicationSnapshotAsync(Guid applicationId, Guid userId, CancellationToken cancellationToken)
    {
        var application = await dbContext.ProjectApplications
            .Include(item => item.Project)
            .FirstOrDefaultAsync(item => item.Id == applicationId, cancellationToken)
            ?? throw new ApiException("Application not found", HttpStatusCode.NotFound);

        if (application.ApplicantUserId != userId)
        {
            throw new ApiException("You do not have permission to generate a cover letter for this application", HttpStatusCode.Forbidden);
        }

        return JsonSerializer.Serialize(new
        {
            application.Id,
            application.ProjectId,
            ProjectTitle = application.Project.Title,
            ProjectSummary = application.Project.Summary,
            application.CoverLetter
        }, JsonOptions);
    }

    private static string CreateProjectSnapshot(AIProjectContext project)
    {
        return JsonSerializer.Serialize(project, JsonOptions);
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
