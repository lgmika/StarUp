using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Subscriptions.Dtos;

public sealed record UsageQuotaDto(string ResourceKey, int Limit);

public sealed record SubscriptionPlanDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    decimal MonthlyPrice,
    string Currency,
    IReadOnlyCollection<UsageQuotaDto> Quotas);

public sealed record SubscriptionDto(
    Guid Id,
    Guid PlanId,
    string PlanCode,
    string PlanName,
    SubscriptionStatus Status,
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset CurrentPeriodEnd,
    DateTimeOffset? TrialEndsAt,
    DateTimeOffset? CancelledAt,
    IReadOnlyCollection<UsageQuotaDto> Quotas);

public sealed record CheckoutRequest(
    Guid PlanId,
    string? SuccessUrl,
    string? CancelUrl);

public sealed record CheckoutResponse(
    Guid TransactionId,
    string Provider,
    string CheckoutSessionId,
    string CheckoutUrl);

public sealed record PaymentWebhookResult(
    string EventId,
    string EventType,
    bool Processed,
    bool Duplicate);
