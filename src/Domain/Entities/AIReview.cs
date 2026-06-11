namespace StartupConnect.Domain.Entities;

public sealed class AIReview : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }

    public User RequestedByUser { get; set; } = null!;

    public Guid AIRequestId { get; set; }

    public AIRequest AIRequest { get; set; } = null!;

    public int QualityScore { get; set; }

    public string MissingInformationJson { get; set; } = "[]";

    public string RiskFlagsJson { get; set; } = "[]";

    public string SuggestionsJson { get; set; } = "[]";

    public string Summary { get; set; } = string.Empty;
}

