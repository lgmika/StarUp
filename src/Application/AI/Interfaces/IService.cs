using System.Security.Claims;
using StartupConnect.Application.AI.Dtos;

namespace StartupConnect.Application.AI.Interfaces;

public interface IAIService
{
    Task<IReadOnlyCollection<AIRecommendationDto>> CreateProjectSuggestionsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task<AIReviewDto> CreateProjectReviewAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AIReviewDto>> GetProjectReviewsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task<AIReviewDto> GetLatestProjectReviewAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task<ApplyAIRecommendationResponse> ApplyRecommendationAsync(ClaimsPrincipal principal, Guid recommendationId, CancellationToken cancellationToken);

    Task<AITextResponse> CreateCoverLetterAsync(ClaimsPrincipal principal, Guid applicationId, CancellationToken cancellationToken);

    Task<AITextResponse> CreateInvestorSummaryAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);
}

