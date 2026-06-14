using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using StartupConnect.Application.Subscriptions.Dtos;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Subscriptions;

namespace StartupConnect.Tests;

public sealed class SubscriptionTests
{
    [Fact]
    public void MockPaymentProvider_Should_Verify_Webhook_Signature()
    {
        const string secret = "test-secret";
        const string payload = "{\"eventId\":\"evt_1\",\"type\":\"checkout.completed\"}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        var provider = new MockPaymentProvider(Options.Create(new PaymentOptions { WebhookSecret = secret }));

        Assert.True(provider.VerifyWebhookSignature(payload, signature));
        Assert.False(provider.VerifyWebhookSignature(payload, "bad-signature"));
    }

    [Fact]
    public void SubscriptionPlanDto_Should_Expose_Usage_Quotas()
    {
        var plan = new SubscriptionPlanDto(
            Guid.NewGuid(),
            "Pro",
            "Pro",
            "More AI requests",
            19,
            "USD",
            [new UsageQuotaDto("ai_requests_monthly", 200)]);

        Assert.Single(plan.Quotas);
        Assert.Equal("ai_requests_monthly", plan.Quotas.First().ResourceKey);
    }

    [Fact]
    public void StripePaymentProvider_Should_Normalize_Checkout_Completed_Webhook()
    {
        var userId = Guid.NewGuid();
        var provider = new StripePaymentProvider(Options.Create(new PaymentOptions()));
        var payload = $$"""
        {
          "id": "evt_checkout_completed",
          "type": "checkout.session.completed",
          "data": {
            "object": {
              "id": "cs_test_123",
              "subscription": "sub_test_123",
              "payment_intent": "pi_test_123",
              "metadata": {
                "userId": "{{userId}}",
                "planCode": "Pro"
              }
            }
          }
        }
        """;

        var normalized = provider.ParseWebhookPayload(payload);

        Assert.Equal("evt_checkout_completed", normalized.EventId);
        Assert.Equal("checkout.completed", normalized.Type);
        Assert.Equal(userId, normalized.UserId);
        Assert.Equal("Pro", normalized.PlanCode);
        Assert.Equal("cs_test_123", normalized.CheckoutSessionId);
        Assert.Equal("sub_test_123", normalized.ProviderSubscriptionId);
        Assert.Equal(SubscriptionStatus.Active, normalized.Status);
    }
}
