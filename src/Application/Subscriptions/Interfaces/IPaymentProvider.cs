using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Subscriptions.Interfaces;

public interface IPaymentProvider
{
    string Name { get; }

    Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(
        Guid userId,
        SubscriptionPlan plan,
        string? successUrl,
        string? cancelUrl,
        CancellationToken cancellationToken);

    bool VerifyWebhookSignature(string payloadJson, string? signature);

    PaymentProviderWebhookPayload ParseWebhookPayload(string payloadJson);
}

public sealed record PaymentCheckoutSession(string SessionId, string CheckoutUrl);

public sealed record PaymentProviderWebhookPayload(
    string EventId,
    string Type,
    Guid? UserId,
    string? PlanCode,
    string? CheckoutSessionId,
    string? ProviderSubscriptionId,
    string? ProviderTransactionId,
    SubscriptionStatus? Status,
    DateTimeOffset? PeriodStart,
    DateTimeOffset? PeriodEnd);
