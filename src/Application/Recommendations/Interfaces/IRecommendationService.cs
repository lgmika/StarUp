using System.Security.Claims;
using StartupConnect.Application.Recommendations.Dtos;

namespace StartupConnect.Application.Recommendations.Interfaces;

public interface IRecommendationService
{
    Task<RecommendationListResponse<ProjectRecommendationDto>> GetProjectRecommendationsAsync(
        ClaimsPrincipal principal,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<RecommendationListResponse<MemberRecommendationDto>> GetMemberRecommendationsAsync(
        ClaimsPrincipal principal,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<RecommendationListResponse<MemberRecommendationDto>> GetRecommendedMembersForProjectAsync(
        ClaimsPrincipal principal,
        Guid projectId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task DismissAsync(ClaimsPrincipal principal, Guid recommendationId, CancellationToken cancellationToken);
}
