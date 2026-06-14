using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StartupConnect.Application.Subscriptions.Interfaces;
using StartupConnect.Domain.Entities;

namespace StartupConnect.Infrastructure.Subscriptions;

public sealed class ConfiguredPaymentProvider(IOptions<PaymentOptions> optionsAccessor) : IPaymentProvider
{
    private readonly PaymentOptions options = optionsAccessor.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Name => options.Provider;

    public Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(
        Guid userId,
        SubscriptionPlan plan,
        string? successUrl,
        string? cancelUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ProductionCheckoutBaseUrl))
        {
            throw new InvalidOperationException("Payments:ProductionCheckoutBaseUrl is required for the configured payment provider.");
        }

        var sessionId = $"{Name.ToLowerInvariant()}_{Guid.NewGuid():N}";
        var query = new Dictionary<string, string?>
        {
            ["sessionId"] = sessionId,
            ["userId"] = userId.ToString("D"),
            ["planCode"] = plan.Code,
            ["amount"] = plan.MonthlyPrice.ToString("0.00"),
            ["currency"] = plan.Currency,
            ["successUrl"] = successUrl,
            ["cancelUrl"] = cancelUrl
        };
        var checkoutUrl = BuildUrl(options.ProductionCheckoutBaseUrl, query);
        return Task.FromResult(new PaymentCheckoutSession(sessionId, checkoutUrl));
    }

    public bool VerifyWebhookSignature(string payloadJson, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var secret = string.IsNullOrWhiteSpace(options.WebhookSecret)
            ? "DEV_ONLY_PaymentWebhookSecret_ReplaceInProduction"
            : options.WebhookSecret;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant()));
    }

    public PaymentProviderWebhookPayload ParseWebhookPayload(string payloadJson)
    {
        return JsonSerializer.Deserialize<PaymentProviderWebhookPayload>(payloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Invalid payment webhook payload.");
    }

    private static string BuildUrl(string baseUrl, IReadOnlyDictionary<string, string?> query)
    {
        var builder = new StringBuilder(baseUrl.TrimEnd('/'));
        builder.Append(baseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?');
        builder.Append(string.Join("&", query
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value!)}")));
        return builder.ToString();
    }
}
