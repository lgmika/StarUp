namespace StartupConnect.Domain.Entities;

public sealed class AIRecommendation : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }

    public User RequestedByUser { get; set; } = null!;

    public Guid AIRequestId { get; set; }

    public AIRequest AIRequest { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string TargetField { get; set; } = string.Empty;

    public bool IsApplied { get; set; }

    public DateTimeOffset? AppliedAt { get; set; }
}

