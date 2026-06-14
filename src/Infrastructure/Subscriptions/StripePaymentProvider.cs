using System.Text.Json;
using Microsoft.Extensions.Options;
using StartupConnect.Application.Subscriptions.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using Stripe;
using Stripe.Checkout;

namespace StartupConnect.Infrastructure.Subscriptions;

public sealed class StripePaymentProvider(IOptions<PaymentOptions> optionsAccessor) : IPaymentProvider
{
    private readonly PaymentOptions options = optionsAccessor.Value;

    public string Name => "Stripe";

    public async Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(
        Guid userId,
        SubscriptionPlan plan,
        string? successUrl,
        string? cancelUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("Payments:ApiKey is required when Payments:Provider is Stripe.");
        }

        var service = new SessionService(new StripeClient(options.ApiKey));
        var session = await service.CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            SuccessUrl = NormalizeReturnUrl(successUrl, true),
            CancelUrl = NormalizeReturnUrl(cancelUrl, false),
            ClientReferenceId = userId.ToString("D"),
            Metadata = BuildMetadata(userId, plan),
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = BuildMetadata(userId, plan)
            },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = plan.Currency.ToLowerInvariant(),
                        UnitAmountDecimal = decimal.Round(plan.MonthlyPrice * 100, 0),
                        Recurring = new SessionLineItemPriceDataRecurringOptions
                        {
                            Interval = "month"
                        },
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = plan.Name,
                            Description = plan.Description,
                            Metadata = BuildMetadata(userId, plan)
                        }
                    }
                }
            ]
        }, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe checkout session did not return a checkout URL.");
        }

        return new PaymentCheckoutSession(session.Id, session.Url);
    }

    public bool VerifyWebhookSignature(string payloadJson, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(options.WebhookSecret))
        {
            return false;
        }

        try
        {
            EventUtility.ConstructEvent(payloadJson, signature, options.WebhookSecret);
            return true;
        }
        catch (StripeException)
        {
            return false;
        }
    }

    public PaymentProviderWebhookPayload ParseWebhookPayload(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;
        var eventId = root.GetProperty("id").GetString() ?? throw new InvalidOperationException("Stripe event id is missing.");
        var stripeType = root.GetProperty("type").GetString() ?? throw new InvalidOperationException("Stripe event type is missing.");
        var dataObject = root.GetProperty("data").GetProperty("object");
        var metadata = TryGetObject(dataObject, "metadata");

        return stripeType switch
        {
            "checkout.session.completed" => BuildCheckoutCompleted(eventId, dataObject, metadata),
            "customer.subscription.updated" => BuildSubscriptionChanged(eventId, "subscription.updated", dataObject, metadata),
            "customer.subscription.deleted" => BuildSubscriptionChanged(eventId, "subscription.cancelled", dataObject, metadata),
            "invoice.payment_failed" => BuildInvoiceFailed(eventId, dataObject, metadata),
            _ => new PaymentProviderWebhookPayload(eventId, stripeType, null, null, null, null, null, null, null, null)
        };
    }

    private PaymentProviderWebhookPayload BuildCheckoutCompleted(string eventId, JsonElement session, JsonElement? metadata)
    {
        return new PaymentProviderWebhookPayload(
            eventId,
            "checkout.completed",
            ReadUserId(metadata),
            ReadMetadata(metadata, "planCode"),
            ReadString(session, "id"),
            ReadString(session, "subscription"),
            ReadString(session, "payment_intent"),
            SubscriptionStatus.Active,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMonths(1));
    }

    private PaymentProviderWebhookPayload BuildSubscriptionChanged(
        string eventId,
        string normalizedType,
        JsonElement subscription,
        JsonElement? metadata)
    {
        return new PaymentProviderWebhookPayload(
            eventId,
            normalizedType,
            ReadUserId(metadata),
            ReadMetadata(metadata, "planCode"),
            null,
            ReadString(subscription, "id"),
            null,
            MapStripeSubscriptionStatus(ReadString(subscription, "status")),
            ReadUnixTime(subscription, "current_period_start"),
            ReadUnixTime(subscription, "current_period_end"));
    }

    private PaymentProviderWebhookPayload BuildInvoiceFailed(string eventId, JsonElement invoice, JsonElement? metadata)
    {
        return new PaymentProviderWebhookPayload(
            eventId,
            "invoice.payment_failed",
            ReadUserId(metadata),
            ReadMetadata(metadata, "planCode"),
            null,
            ReadString(invoice, "subscription"),
            ReadString(invoice, "payment_intent"),
            SubscriptionStatus.PastDue,
            null,
            null);
    }

    private string NormalizeReturnUrl(string? configuredUrl, bool success)
    {
        if (!string.IsNullOrWhiteSpace(configuredUrl))
        {
            return configuredUrl;
        }

        if (string.IsNullOrWhiteSpace(options.CheckoutBaseUrl))
        {
            throw new InvalidOperationException("Payments:CheckoutBaseUrl is required for Stripe checkout return URLs.");
        }

        var separator = options.CheckoutBaseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{options.CheckoutBaseUrl}{separator}status={(success ? "success" : "cancelled")}&session_id={{CHECKOUT_SESSION_ID}}";
    }

    private static Dictionary<string, string> BuildMetadata(Guid userId, SubscriptionPlan plan)
    {
        return new Dictionary<string, string>
        {
            ["userId"] = userId.ToString("D"),
            ["planId"] = plan.Id.ToString("D"),
            ["planCode"] = plan.Code
        };
    }

    private static SubscriptionStatus? MapStripeSubscriptionStatus(string? status)
    {
        return status?.ToLowerInvariant() switch
        {
            "trialing" => SubscriptionStatus.Trialing,
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" or "cancelled" or "unpaid" => SubscriptionStatus.Cancelled,
            "incomplete_expired" => SubscriptionStatus.Expired,
            _ => null
        };
    }

    private static Guid? ReadUserId(JsonElement? metadata)
    {
        return Guid.TryParse(ReadMetadata(metadata, "userId"), out var userId) ? userId : null;
    }

    private static string? ReadMetadata(JsonElement? metadata, string key)
    {
        if (metadata is null || metadata.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return metadata.Value.TryGetProperty(key, out var value) ? value.GetString() : null;
    }

    private static JsonElement? TryGetObject(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Object
            ? value
            : null;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static DateTimeOffset? ReadUnixTime(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return value.TryGetInt64(out var seconds) ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;
    }
}
