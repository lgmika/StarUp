namespace StartupConnect.Infrastructure.AI;

public sealed record AIProjectContext(
    Guid ProjectId,
    string Title,
    string Summary,
    string Problem,
    string Solution,
    string TargetMarket,
    string BusinessModel,
    string Stage,
    IReadOnlyCollection<string> RequiredRoles);

public sealed record AIProjectSuggestionResult(
    string Title,
    string TargetField,
    string Content);

public sealed record AIProjectReviewResult(
    int QualityScore,
    IReadOnlyCollection<string> MissingInformation,
    IReadOnlyCollection<string> RiskFlags,
    IReadOnlyCollection<string> Suggestions,
    string Summary);

public sealed record AITextResult(string Content);
