namespace StartupConnect.Infrastructure.Subscriptions;

public sealed class PaymentOptions
{
    public string Provider { get; set; } = "Mock";

    public string WebhookSecret { get; set; } = "DEV_ONLY_PaymentWebhookSecret_ReplaceInProduction";

    public string CheckoutBaseUrl { get; set; } = "http://localhost:3000/billing/checkout";

    public string ProductionCheckoutBaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
