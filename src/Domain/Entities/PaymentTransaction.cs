using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class PaymentTransaction : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid PlanId { get; set; }

    public SubscriptionPlan Plan { get; set; } = null!;

    public Guid? SubscriptionId { get; set; }

    public UserSubscription? Subscription { get; set; }

    public string Provider { get; set; } = "Mock";

    public string ProviderCheckoutSessionId { get; set; } = string.Empty;

    public string? ProviderTransactionId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;
}
