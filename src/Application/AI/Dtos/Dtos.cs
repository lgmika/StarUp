namespace StartupConnect.Application.AI.Dtos;

public sealed record AIRecommendationDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Content,
    string TargetField,
    bool IsApplied,
    DateTimeOffset CreatedAt);

public sealed record AIReviewDto(
    Guid Id,
    Guid ProjectId,
    int QualityScore,
    IReadOnlyCollection<string> MissingInformation,
    IReadOnlyCollection<string> RiskFlags,
    IReadOnlyCollection<string> Suggestions,
    string Summary,
    DateTimeOffset CreatedAt);

public sealed record AITextResponse(string Content);

public sealed record ApplyAIRecommendationResponse(
    Guid RecommendationId,
    bool IsApplied,
    DateTimeOffset AppliedAt);

