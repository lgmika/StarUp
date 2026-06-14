namespace StartupConnect.Domain.Entities;

public sealed class PaymentWebhookEvent : BaseEntity
{
    public string Provider { get; set; } = "Mock";

    public string ProviderEventId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public bool IsProcessed { get; set; }

    public string? ProcessingError { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }
}
