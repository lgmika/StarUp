namespace StartupConnect.Infrastructure.Subscriptions;

public static class PaymentReturnUrlPolicy
{
    public static string Normalize(string checkoutBaseUrl, string? requestedUrl, bool success)
    {
        if (!Uri.TryCreate(checkoutBaseUrl, UriKind.Absolute, out var checkoutBaseUri) ||
            (checkoutBaseUri.Scheme != Uri.UriSchemeHttp && checkoutBaseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Payments:CheckoutBaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        if (!string.IsNullOrWhiteSpace(requestedUrl))
        {
            if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out var returnUri) ||
                !HasSameOrigin(checkoutBaseUri, returnUri))
            {
                throw new InvalidOperationException("Checkout return URLs must use the configured payment origin.");
            }

            return requestedUrl;
        }

        var separator = checkoutBaseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{checkoutBaseUrl}{separator}status={(success ? "success" : "cancelled")}&session_id={{CHECKOUT_SESSION_ID}}";
    }

    private static bool HasSameOrigin(Uri expected, Uri candidate)
    {
        return expected.Scheme.Equals(candidate.Scheme, StringComparison.OrdinalIgnoreCase) &&
            expected.Host.Equals(candidate.Host, StringComparison.OrdinalIgnoreCase) &&
            expected.Port == candidate.Port;
    }
}
