using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StartupConnect.Application.Subscriptions.Interfaces;
using StartupConnect.Domain.Entities;

namespace StartupConnect.Infrastructure.Subscriptions;

public sealed class MockPaymentProvider(IOptions<PaymentOptions> optionsAccessor) : IPaymentProvider
{
    private readonly PaymentOptions options = optionsAccessor.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Name => "Mock";

    public Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(
        Guid userId,
        SubscriptionPlan plan,
        string? successUrl,
        string? cancelUrl,
        CancellationToken cancellationToken)
    {
        var sessionId = $"mock_cs_{Guid.NewGuid():N}";
        var checkoutUrl = $"{options.CheckoutBaseUrl.TrimEnd('/')}?sessionId={sessionId}&plan={Uri.EscapeDataString(plan.Code)}";
        return Task.FromResult(new PaymentCheckoutSession(sessionId, checkoutUrl));
    }

    public Task CancelSubscriptionAsync(string? providerSubscriptionId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ResumeSubscriptionAsync(string? providerSubscriptionId, CancellationToken cancellationToken) => Task.CompletedTask;

    public bool VerifyWebhookSignature(string payloadJson, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var expected = Sign(payloadJson);
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature));
    }

    public PaymentProviderWebhookPayload ParseWebhookPayload(string payloadJson)
    {
        return JsonSerializer.Deserialize<PaymentProviderWebhookPayload>(payloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Invalid payment webhook payload.");
    }

    private string Sign(string payloadJson)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.WebhookSecret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson))).ToLowerInvariant();
    }
}
