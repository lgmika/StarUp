namespace StartupConnect.Domain.Entities;

public sealed class UsageQuota : BaseEntity
{
    public Guid PlanId { get; set; }

    public SubscriptionPlan Plan { get; set; } = null!;

    public string ResourceKey { get; set; } = string.Empty;

    public int Limit { get; set; }
}
