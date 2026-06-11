using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class ProjectModerationReview : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid ModeratorUserId { get; set; }

    public User ModeratorUser { get; set; } = null!;

    public ModerationDecision Decision { get; set; }

    public string Reason { get; set; } = string.Empty;

    public int? AIQualityScoreSnapshot { get; set; }

    public string? AIRiskFlagsSnapshotJson { get; set; }
}

