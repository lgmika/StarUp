using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class UserSubscription : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid PlanId { get; set; }

    public SubscriptionPlan Plan { get; set; } = null!;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;

    public string Provider { get; set; } = "Mock";

    public string? ProviderSubscriptionId { get; set; }

    public DateTimeOffset? TrialEndsAt { get; set; }

    public DateTimeOffset CurrentPeriodStart { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset CurrentPeriodEnd { get; set; } = DateTimeOffset.UtcNow.AddMonths(1);

    public DateTimeOffset? CancelledAt { get; set; }
}
