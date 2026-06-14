using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Recommendations.Dtos;

public sealed record RecommendationBreakdownItemDto(string Key, int Points, string Explanation);

public sealed record ProjectRecommendationDto(
    Guid RecommendationId,
    Guid ProjectId,
    string Title,
    string Summary,
    ProjectStage Stage,
    ProjectVisibility Visibility,
    int Score,
    IReadOnlyCollection<RecommendationBreakdownItemDto> Breakdown);

public sealed record MemberRecommendationDto(
    Guid RecommendationId,
    Guid ProjectId,
    string ProjectTitle,
    Guid UserId,
    string FullName,
    string Headline,
    string? Location,
    IReadOnlyCollection<string> MatchedSkills,
    int Score,
    IReadOnlyCollection<RecommendationBreakdownItemDto> Breakdown);

public sealed record RecommendationListResponse<T>(
    IReadOnlyCollection<T> Items,
    int Total,
    int Page,
    int PageSize);
