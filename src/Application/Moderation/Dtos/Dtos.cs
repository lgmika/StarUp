using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Moderation.Dtos;

public sealed record ModeratorDashboardDto(
    int PendingProjects,
    int PublishedProjects,
    int RejectedProjects,
    int HiddenProjects,
    int PendingReports);

public sealed record ModeratorProjectQueueItemDto(
    Guid ProjectId,
    string Title,
    string Summary,
    ProjectStatus Status,
    ProjectStage Stage,
    int? LatestAIQualityScore,
    IReadOnlyCollection<string> LatestAIRiskFlags,
    DateTimeOffset? SubmittedAt);

public sealed record ModeratorProjectDetailDto(
    Guid ProjectId,
    string Title,
    string Summary,
    string Problem,
    string Solution,
    ProjectStatus Status,
    ProjectStage Stage,
    Guid OwnerUserId,
    string OwnerEmail,
    int? LatestAIQualityScore,
    IReadOnlyCollection<string> LatestAIRiskFlags,
    IReadOnlyCollection<ModerationReviewDto> ModerationHistory);

public sealed record ModerationReviewDto(
    Guid Id,
    ModerationDecision Decision,
    string Reason,
    int? AIQualityScoreSnapshot,
    DateTimeOffset CreatedAt);

public sealed record ModerationDecisionRequest(string Reason);

