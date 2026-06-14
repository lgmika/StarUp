using Microsoft.Extensions.Hosting;
using System.Net.Mail;

namespace StartupConnect.Api.Security;

public static class SecurityConfigurationValidator
{
    private const int MinimumSigningKeyLength = 32;

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        var jwtSigningKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(jwtSigningKey) || jwtSigningKey.Length < MinimumSigningKeyLength)
        {
            throw new InvalidOperationException($"Jwt:SigningKey must be at least {MinimumSigningKeyLength} characters.");
        }

        var refreshCookie = configuration.GetSection("Security:RefreshTokenCookie").Get<RefreshTokenCookieSettings>() ?? new RefreshTokenCookieSettings();
        if (refreshCookie.Enabled && refreshCookie.SameSite.Equals("None", StringComparison.OrdinalIgnoreCase) && !refreshCookie.Secure)
        {
            throw new InvalidOperationException("Security:RefreshTokenCookie:Secure must be true when SameSite is None.");
        }

        if (environment.IsDevelopment())
        {
            return;
        }

        if (jwtSigningKey.Contains("DEV_ONLY", StringComparison.OrdinalIgnoreCase) ||
            jwtSigningKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Jwt:SigningKey must be replaced before running outside Development.");
        }

        var allowedHosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
        {
            throw new InvalidOperationException("AllowedHosts must be restricted before running outside Development.");
        }

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length == 0 ||
            allowedOrigins.Any(origin => origin == "*" || origin.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must contain production origins before running outside Development.");
        }

        var paymentProvider = configuration["Payments:Provider"] ?? "Mock";
        if (environment.IsProduction() && paymentProvider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Payments:Provider must use a real provider before running in Production.");
        }

        if (paymentProvider.Equals("Stripe", StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = configuration["Payments:ApiKey"];
            var webhookSecret = configuration["Payments:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(apiKey) || !apiKey.StartsWith("sk_", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Payments:ApiKey must be a Stripe secret key when Payments:Provider is Stripe.");
            }

            if (string.IsNullOrWhiteSpace(webhookSecret) ||
                webhookSecret.Contains("DEV_ONLY", StringComparison.OrdinalIgnoreCase) ||
                !webhookSecret.StartsWith("whsec_", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Payments:WebhookSecret must be a Stripe webhook signing secret when Payments:Provider is Stripe.");
            }
        }

        ValidateProductionEmail(configuration, environment);
        ValidateProductionFileStorage(configuration, environment);
        ValidateProductionOperations(configuration, environment);
    }

    private static void ValidateProductionEmail(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var provider = configuration["Email:Provider"] ?? "Development";
        if (!provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Email:Provider must be Smtp before running in Production.");
        }

        var fromEmail = configuration["Email:FromEmail"];
        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("Email:FromEmail is required before running in Production.");
        }

        MailAddress fromAddress;
        try
        {
            fromAddress = new MailAddress(fromEmail);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Email:FromEmail must be a valid email address.", exception);
        }

        var fromDomain = fromAddress.Host;
        if (fromDomain.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            fromDomain.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Email:FromEmail must use a verified production sender domain.");
        }

        var requireVerifiedDomain = bool.TryParse(configuration["Email:RequireVerifiedSenderDomain"], out var requireVerified) && requireVerified;
        var verifiedDomain = configuration["Email:VerifiedSenderDomain"];
        if (requireVerifiedDomain && string.IsNullOrWhiteSpace(verifiedDomain))
        {
            throw new InvalidOperationException("Email:VerifiedSenderDomain is required when Email:RequireVerifiedSenderDomain is true.");
        }

        if (!string.IsNullOrWhiteSpace(verifiedDomain) &&
            !fromDomain.Equals(verifiedDomain.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Email:FromEmail must match Email:VerifiedSenderDomain.");
        }

        var appBaseUrl = configuration["Email:AppBaseUrl"];
        if (!Uri.TryCreate(appBaseUrl, UriKind.Absolute, out var appUri) ||
            appUri.Scheme != Uri.UriSchemeHttps ||
            appUri.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Email:AppBaseUrl must be an HTTPS production URL.");
        }

        if (string.IsNullOrWhiteSpace(configuration["Email:Smtp:Host"]) ||
            string.IsNullOrWhiteSpace(configuration["Email:Smtp:Username"]) ||
            string.IsNullOrWhiteSpace(configuration["Email:Smtp:Password"]))
        {
            throw new InvalidOperationException("Email:Smtp:Host, Username, and Password are required before running in Production.");
        }
    }

    private static void ValidateProductionFileStorage(IConfiguration configuration, IHostEnvironment environment)
    {
        var provider = configuration["FileStorage:Provider"] ?? "Local";

        if (environment.IsProduction() && provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("FileStorage:Provider must use cloud storage before running in Production.");
        }

        if (!provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(configuration["FileStorage:S3:BucketName"]))
        {
            throw new InvalidOperationException("FileStorage:S3:BucketName is required when FileStorage:Provider is S3.");
        }

        var region = configuration["FileStorage:S3:Region"];
        var serviceUrl = configuration["FileStorage:S3:ServiceUrl"];
        if (string.IsNullOrWhiteSpace(region) && string.IsNullOrWhiteSpace(serviceUrl))
        {
            throw new InvalidOperationException("FileStorage:S3:Region or FileStorage:S3:ServiceUrl is required when FileStorage:Provider is S3.");
        }

        var accessKey = configuration["FileStorage:S3:AccessKeyId"];
        var secretKey = configuration["FileStorage:S3:SecretAccessKey"];
        if (string.IsNullOrWhiteSpace(accessKey) != string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("FileStorage:S3:AccessKeyId and SecretAccessKey must be configured together, or both omitted to use IAM role/default credentials.");
        }
    }

    private static void ValidateProductionOperations(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var rateLimitingEnabled = !bool.TryParse(configuration["RateLimiting:Enabled"], out var enabled) || enabled;
        if (!rateLimitingEnabled)
        {
            throw new InvalidOperationException("RateLimiting:Enabled must be true before running in Production.");
        }

        var requestLoggingEnabled = !bool.TryParse(configuration["Observability:EnableRequestLogging"], out var loggingEnabled) || loggingEnabled;
        if (!requestLoggingEnabled)
        {
            throw new InvalidOperationException("Observability:EnableRequestLogging must be true before running in Production.");
        }

        if (int.TryParse(configuration["RateLimiting:PermitLimit"], out var permitLimit) && permitLimit <= 0)
        {
            throw new InvalidOperationException("RateLimiting:PermitLimit must be greater than zero.");
        }

        if (int.TryParse(configuration["RateLimiting:AuthPermitLimit"], out var authPermitLimit) && authPermitLimit <= 0)
        {
            throw new InvalidOperationException("RateLimiting:AuthPermitLimit must be greater than zero.");
        }
    }
}
