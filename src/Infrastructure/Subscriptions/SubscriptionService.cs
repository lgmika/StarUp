using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Realtime;
using StartupConnect.Application.Realtime.Interfaces;
using StartupConnect.Application.Subscriptions.Dtos;
using StartupConnect.Application.Subscriptions.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;

namespace StartupConnect.Infrastructure.Subscriptions;

public sealed class SubscriptionService(
    AppDbContext dbContext,
    IPaymentProvider paymentProvider,
    IRealtimeNotifier realtimeNotifier) : ISubscriptionService
{
    public async Task<IReadOnlyCollection<SubscriptionPlanDto>> GetPlansAsync(CancellationToken cancellationToken)
    {
        var plans = await dbContext.SubscriptionPlans
            .Where(plan => plan.IsActive)
            .OrderBy(plan => plan.MonthlyPrice)
            .ToArrayAsync(cancellationToken);

        return await MapPlansAsync(plans, cancellationToken);
    }

    public async Task<SubscriptionDto> GetMySubscriptionAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var subscription = await GetCurrentSubscriptionAsync(userId, cancellationToken);
        return await MapSubscriptionAsync(subscription, cancellationToken);
    }

    public async Task<CheckoutResponse> CreateCheckoutAsync(ClaimsPrincipal principal, CheckoutRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var plan = await dbContext.SubscriptionPlans.FirstOrDefaultAsync(item => item.Id == request.PlanId && item.IsActive, cancellationToken)
            ?? throw new ApiException("Subscription plan not found", HttpStatusCode.NotFound);

        if (plan.Code.Equals("Free", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException("Free plan does not require checkout", HttpStatusCode.BadRequest);
        }

        var session = await paymentProvider.CreateCheckoutSessionAsync(userId, plan, request.SuccessUrl, request.CancelUrl, cancellationToken);
        var transaction = new PaymentTransaction
        {
            UserId = userId,
            PlanId = plan.Id,
            Provider = paymentProvider.Name,
            ProviderCheckoutSessionId = session.SessionId,
            Amount = plan.MonthlyPrice,
            Currency = plan.Currency,
            Status = PaymentTransactionStatus.Pending
        };

        dbContext.PaymentTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CheckoutResponse(transaction.Id, paymentProvider.Name, session.SessionId, session.CheckoutUrl);
    }

    public async Task<SubscriptionDto> CancelAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var subscription = await GetCurrentSubscriptionAsync(userId, cancellationToken);
        if (subscription.Status is SubscriptionStatus.Active or SubscriptionStatus.Trialing or SubscriptionStatus.PastDue)
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelledAt = DateTimeOffset.UtcNow;
            subscription.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await NotifyAndReturnSubscriptionAsync(userId, subscription, cancellationToken);
    }

    public async Task<SubscriptionDto> ResumeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var subscription = await GetCurrentSubscriptionAsync(userId, cancellationToken);
        if (subscription.Status == SubscriptionStatus.Cancelled && subscription.CurrentPeriodEnd > DateTimeOffset.UtcNow)
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.CancelledAt = null;
            subscription.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await NotifyAndReturnSubscriptionAsync(userId, subscription, cancellationToken);
    }

    public async Task<PaymentWebhookResult> HandleWebhookAsync(string payloadJson, string? signature, CancellationToken cancellationToken)
    {
        if (!paymentProvider.VerifyWebhookSignature(payloadJson, signature))
        {
            throw new ApiException("Invalid payment webhook signature", HttpStatusCode.Unauthorized);
        }

        PaymentProviderWebhookPayload payload;
        try
        {
            payload = paymentProvider.ParseWebhookPayload(payloadJson);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.Text.Json.JsonException)
        {
            throw new ApiException(exception.Message, HttpStatusCode.BadRequest);
        }

        var duplicate = await dbContext.PaymentWebhookEvents.AnyAsync(
            item => item.Provider == paymentProvider.Name && item.ProviderEventId == payload.EventId,
            cancellationToken);
        if (duplicate)
        {
            return new PaymentWebhookResult(payload.EventId, payload.Type, Processed: false, Duplicate: true);
        }

        var webhook = new PaymentWebhookEvent
        {
            Provider = paymentProvider.Name,
            ProviderEventId = payload.EventId,
            EventType = payload.Type,
            PayloadJson = payloadJson
        };
        dbContext.PaymentWebhookEvents.Add(webhook);

        try
        {
            await ApplyWebhookAsync(payload, cancellationToken);
            webhook.IsProcessed = true;
            webhook.ProcessedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return new PaymentWebhookResult(payload.EventId, payload.Type, Processed: true, Duplicate: false);
        }
        catch (Exception exception)
        {
            webhook.ProcessingError = exception.Message;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task ApplyWebhookAsync(PaymentProviderWebhookPayload payload, CancellationToken cancellationToken)
    {
        if (payload.Type is "checkout.completed" or "subscription.updated")
        {
            var userId = payload.UserId ?? throw new ApiException("Webhook user id is required", HttpStatusCode.BadRequest);
            var plan = await dbContext.SubscriptionPlans.FirstOrDefaultAsync(plan => plan.Code == payload.PlanCode, cancellationToken)
                ?? throw new ApiException("Webhook plan code is invalid", HttpStatusCode.BadRequest);
            var subscription = await GetOrCreateSubscriptionAsync(userId, plan, cancellationToken);
            subscription.PlanId = plan.Id;
            subscription.Provider = paymentProvider.Name;
            subscription.ProviderSubscriptionId = payload.ProviderSubscriptionId;
            subscription.Status = payload.Status ?? SubscriptionStatus.Active;
            subscription.CurrentPeriodStart = payload.PeriodStart ?? DateTimeOffset.UtcNow;
            subscription.CurrentPeriodEnd = payload.PeriodEnd ?? DateTimeOffset.UtcNow.AddMonths(1);
            subscription.UpdatedAt = DateTimeOffset.UtcNow;

            if (!string.IsNullOrWhiteSpace(payload.CheckoutSessionId))
            {
                var transaction = await dbContext.PaymentTransactions.FirstOrDefaultAsync(
                    item => item.ProviderCheckoutSessionId == payload.CheckoutSessionId,
                    cancellationToken);
                if (transaction is not null)
                {
                    transaction.Subscription = subscription;
                    transaction.ProviderTransactionId = payload.ProviderTransactionId;
                    transaction.Status = PaymentTransactionStatus.Succeeded;
                    transaction.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            AddAudit(userId, "Payment.Webhook.SubscriptionUpdated", "UserSubscription", subscription.Id, payload.Type);
            await realtimeNotifier.NotifyUserAsync(
                userId,
                RealtimeEventNames.BillingSubscriptionChanged,
                await MapSubscriptionAsync(subscription, cancellationToken),
                cancellationToken);
        }
        else if (payload.Type == "invoice.payment_failed")
        {
            await UpdateStatusAsync(payload, SubscriptionStatus.PastDue, cancellationToken);
        }
        else if (payload.Type == "subscription.cancelled")
        {
            await UpdateStatusAsync(payload, SubscriptionStatus.Cancelled, cancellationToken);
        }
    }

    private async Task UpdateStatusAsync(PaymentProviderWebhookPayload payload, SubscriptionStatus status, CancellationToken cancellationToken)
    {
        if (payload.UserId is null)
        {
            throw new ApiException("Webhook user id is required", HttpStatusCode.BadRequest);
        }

        var subscription = await GetCurrentSubscriptionAsync(payload.UserId.Value, cancellationToken);
        subscription.Status = status;
        subscription.CancelledAt = status == SubscriptionStatus.Cancelled ? DateTimeOffset.UtcNow : subscription.CancelledAt;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(payload.UserId.Value, $"Payment.Webhook.{status}", "UserSubscription", subscription.Id, payload.Type);
        await realtimeNotifier.NotifyUserAsync(
            payload.UserId.Value,
            RealtimeEventNames.BillingSubscriptionChanged,
            await MapSubscriptionAsync(subscription, cancellationToken),
            cancellationToken);
    }

    private async Task<UserSubscription> GetCurrentSubscriptionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.UserSubscriptions
            .Include(item => item.Plan)
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is not null)
        {
            return subscription;
        }

        var freePlan = await dbContext.SubscriptionPlans.FirstAsync(item => item.Code == "Free", cancellationToken);
        subscription = new UserSubscription
        {
            UserId = userId,
            PlanId = freePlan.Id,
            Status = SubscriptionStatus.Active,
            Provider = "System",
            CurrentPeriodStart = DateTimeOffset.UtcNow,
            CurrentPeriodEnd = DateTimeOffset.UtcNow.AddYears(10)
        };
        dbContext.UserSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);
        subscription.Plan = freePlan;
        return subscription;
    }

    private async Task<UserSubscription> GetOrCreateSubscriptionAsync(Guid userId, SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.UserSubscriptions
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is not null)
        {
            return subscription;
        }

        subscription = new UserSubscription { UserId = userId, PlanId = plan.Id };
        dbContext.UserSubscriptions.Add(subscription);
        return subscription;
    }

    private async Task<IReadOnlyCollection<SubscriptionPlanDto>> MapPlansAsync(IReadOnlyCollection<SubscriptionPlan> plans, CancellationToken cancellationToken)
    {
        var planIds = plans.Select(plan => plan.Id).ToArray();
        var quotas = await dbContext.UsageQuotas
            .Where(quota => planIds.Contains(quota.PlanId))
            .GroupBy(quota => quota.PlanId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(quota => new UsageQuotaDto(quota.ResourceKey, quota.Limit)).ToArray(), cancellationToken);

        return plans.Select(plan => new SubscriptionPlanDto(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.Description,
            plan.MonthlyPrice,
            plan.Currency,
            quotas.TryGetValue(plan.Id, out var planQuotas) ? planQuotas : []))
            .ToArray();
    }

    private async Task<SubscriptionDto> MapSubscriptionAsync(UserSubscription subscription, CancellationToken cancellationToken)
    {
        var quotas = await dbContext.UsageQuotas
            .Where(quota => quota.PlanId == subscription.PlanId)
            .OrderBy(quota => quota.ResourceKey)
            .Select(quota => new UsageQuotaDto(quota.ResourceKey, quota.Limit))
            .ToArrayAsync(cancellationToken);

        var plan = subscription.Plan ?? await dbContext.SubscriptionPlans.FirstAsync(item => item.Id == subscription.PlanId, cancellationToken);
        return new SubscriptionDto(
            subscription.Id,
            plan.Id,
            plan.Code,
            plan.Name,
            subscription.Status,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            subscription.TrialEndsAt,
            subscription.CancelledAt,
            quotas);
    }

    private async Task<SubscriptionDto> NotifyAndReturnSubscriptionAsync(Guid userId, UserSubscription subscription, CancellationToken cancellationToken)
    {
        var result = await MapSubscriptionAsync(subscription, cancellationToken);
        await realtimeNotifier.NotifyUserAsync(userId, RealtimeEventNames.BillingSubscriptionChanged, result, cancellationToken);
        return result;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value ?? principal.FindFirst("nameid")?.Value;
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new ApiException("Invalid access token", HttpStatusCode.Unauthorized);
        }

        return userId;
    }

    private void AddAudit(Guid actorUserId, string action, string resourceType, Guid resourceId, string reason)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Reason = reason
        });
    }

}
