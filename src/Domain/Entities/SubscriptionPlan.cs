namespace StartupConnect.Domain.Entities;

public sealed class SubscriptionPlan : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal MonthlyPrice { get; set; }

    public string Currency { get; set; } = "USD";

    public bool IsActive { get; set; } = true;
}
