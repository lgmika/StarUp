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
    public async Task MockPaymentProvider_Should_Support_Subscription_Lifecycle()
    {
        var provider = new MockPaymentProvider(Options.Create(new PaymentOptions()));

        await provider.CancelSubscriptionAsync("sub_mock", CancellationToken.None);
        await provider.ResumeSubscriptionAsync("sub_mock", CancellationToken.None);
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

    [Fact]
    public void StripePaymentProvider_Should_Preserve_Subscription_Id_When_Invoice_Has_No_Metadata()
    {
        var provider = new StripePaymentProvider(Options.Create(new PaymentOptions()));
        const string payload = """
        {
          "id": "evt_invoice_failed",
          "type": "invoice.payment_failed",
          "data": {
            "object": {
              "id": "in_test_123",
              "subscription": "sub_test_123",
              "payment_intent": "pi_test_123"
            }
          }
        }
        """;

        var normalized = provider.ParseWebhookPayload(payload);

        Assert.Equal("invoice.payment_failed", normalized.Type);
        Assert.Null(normalized.UserId);
        Assert.Equal("sub_test_123", normalized.ProviderSubscriptionId);
        Assert.Equal(SubscriptionStatus.PastDue, normalized.Status);
    }

    [Fact]
    public void PaymentReturnUrlPolicy_Should_Reject_External_Origin()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            PaymentReturnUrlPolicy.Normalize(
                "https://app.startupconnect.example/billing/checkout",
                "https://phishing.example/complete",
                success: true));

        Assert.Contains("configured payment origin", exception.Message);
    }

    [Fact]
    public void PaymentReturnUrlPolicy_Should_Create_Safe_Default_Return_Url()
    {
        var result = PaymentReturnUrlPolicy.Normalize(
            "https://app.startupconnect.example/billing/checkout",
            requestedUrl: null,
            success: true);

        Assert.StartsWith("https://app.startupconnect.example/billing/checkout?status=success", result);
        Assert.Contains("{CHECKOUT_SESSION_ID}", result);
    }
}
