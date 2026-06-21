using Microsoft.Extensions.Hosting;
using System.Net;
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

        if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]) ||
            string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
        {
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
        }

        ValidateAuthentication(configuration);
        ValidateCors(configuration, environment);

        var refreshCookie = configuration.GetSection("Security:RefreshTokenCookie").Get<RefreshTokenCookieSettings>() ?? new RefreshTokenCookieSettings();
        if (string.IsNullOrWhiteSpace(refreshCookie.Name) ||
            refreshCookie.Name.Length > 128 ||
            !refreshCookie.Name.All(IsCookieNameCharacter))
        {
            throw new InvalidOperationException("Security:RefreshTokenCookie:Name must be a valid cookie name of at most 128 characters.");
        }

        if (refreshCookie.Enabled &&
            refreshCookie.Name.StartsWith("__Host-", StringComparison.Ordinal) &&
            !refreshCookie.Secure)
        {
            throw new InvalidOperationException("Security:RefreshTokenCookie:Secure must be true for a __Host- cookie.");
        }

        if (refreshCookie.Enabled && refreshCookie.SameSite.Equals("None", StringComparison.OrdinalIgnoreCase) && !refreshCookie.Secure)
        {
            throw new InvalidOperationException("Security:RefreshTokenCookie:Secure must be true when SameSite is None.");
        }

        if (refreshCookie.Days is < 1 or > 90)
        {
            throw new InvalidOperationException("Security:RefreshTokenCookie:Days must be between 1 and 90.");
        }

        if (!refreshCookie.SameSite.Equals("Strict", StringComparison.OrdinalIgnoreCase) &&
            !refreshCookie.SameSite.Equals("Lax", StringComparison.OrdinalIgnoreCase) &&
            !refreshCookie.SameSite.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Security:RefreshTokenCookie:SameSite must be Strict, Lax, or None.");
        }

        ValidateForwardedHeaders(configuration, environment);

        var maxRequestBodySize = configuration.GetValue<long?>("Security:MaxRequestBodySizeBytes") ?? 25 * 1024 * 1024;
        if (maxRequestBodySize is < 1 or > 100 * 1024 * 1024)
        {
            throw new InvalidOperationException("Security:MaxRequestBodySizeBytes must be between 1 byte and 100 MB.");
        }

        ValidateAI(configuration, environment);
        ValidateObservability(configuration);
        ValidateEmail(configuration);
        ValidateEmailOutbox(configuration, environment);
        ValidateFileStorage(configuration);
        ValidateRateLimiting(configuration);
        ValidateBackgroundJobs(configuration);

        var paymentProvider = configuration["Payments:Provider"] ?? "Mock";
        if (!paymentProvider.Equals("Mock", StringComparison.OrdinalIgnoreCase) &&
            !paymentProvider.Equals("Stripe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Payments:Provider '{paymentProvider}' is not supported.");
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

        if (!configuration.GetValue("Security:RequireHttpsRedirection", true))
        {
            throw new InvalidOperationException("Security:RequireHttpsRedirection must be true outside Development.");
        }

        if (!configuration.GetValue("Security:EnableSecurityHeaders", true))
        {
            throw new InvalidOperationException("Security:EnableSecurityHeaders must be true outside Development.");
        }

        if (environment.IsProduction() && !configuration.GetValue("Security:EnableHsts", true))
        {
            throw new InvalidOperationException("Security:EnableHsts must be true in Production.");
        }

        if (refreshCookie.Enabled && !refreshCookie.Secure)
        {
            throw new InvalidOperationException("Security:RefreshTokenCookie:Secure must be true outside Development.");
        }

        if (refreshCookie.Enabled && refreshCookie.SameSite.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Security:RefreshTokenCookie:SameSite cannot be None outside Development without a separate CSRF protection mechanism.");
        }

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

            var checkoutBaseUrl = configuration["Payments:CheckoutBaseUrl"];
            if (!Uri.TryCreate(checkoutBaseUrl, UriKind.Absolute, out var checkoutUri) ||
                (environment.IsProduction() && checkoutUri.Scheme != Uri.UriSchemeHttps) ||
                checkoutUri.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Payments:CheckoutBaseUrl must be a non-local HTTPS URL in Production.");
            }
        }

        ValidateProductionEmail(configuration, environment);
        ValidateProductionFileStorage(configuration, environment);
        ValidateProductionOperations(configuration, environment);
    }

    private static void ValidateForwardedHeaders(IConfiguration configuration, IHostEnvironment environment)
    {
        var enabled = configuration.GetValue("Security:UseForwardedHeaders", true);
        var proxies = configuration.GetSection("Security:KnownProxies").Get<string[]>() ?? [];
        var networks = configuration.GetSection("Security:KnownNetworks").Get<string[]>() ?? [];

        if (!environment.IsDevelopment() && enabled && proxies.Length == 0 && networks.Length == 0)
        {
            throw new InvalidOperationException("Security:KnownProxies or Security:KnownNetworks is required when forwarded headers are enabled outside Development.");
        }

        if (proxies.Any(proxy => !IPAddress.TryParse(proxy, out _)))
        {
            throw new InvalidOperationException("Security:KnownProxies contains an invalid IP address.");
        }

        foreach (var network in networks)
        {
            var separator = network.LastIndexOf('/');
            if (separator <= 0 ||
                !IPAddress.TryParse(network[..separator], out var address) ||
                !int.TryParse(network[(separator + 1)..], out var prefixLength) ||
                prefixLength < 0 ||
                prefixLength > (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128))
            {
                throw new InvalidOperationException("Security:KnownNetworks contains an invalid CIDR network.");
            }
        }
    }

    private static bool IsCookieNameCharacter(char character)
    {
        return char.IsAsciiLetterOrDigit(character) || character is '!' or '#' or '$' or '%' or '&' or '\'' or '*' or '+' or '-' or '.' or '^' or '_' or '`' or '|' or '~';
    }

    private static void ValidateCors(IConfiguration configuration, IHostEnvironment environment)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (!environment.IsDevelopment() && allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must contain production origins before running outside Development.");
        }

        foreach (var origin in allowedOrigins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment) ||
                uri.AbsolutePath != "/" ||
                !origin.Equals(uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cors:AllowedOrigins must contain valid HTTP origins without paths, queries, or fragments.");
            }

            if (!environment.IsDevelopment() &&
                (uri.Scheme != Uri.UriSchemeHttps ||
                 uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                 IPAddress.TryParse(uri.Host, out var address) && IPAddress.IsLoopback(address)))
            {
                throw new InvalidOperationException("Cors:AllowedOrigins must contain non-local HTTPS origins outside Development.");
            }
        }
    }

    private static void ValidateAuthentication(IConfiguration configuration)
    {
        var accessTokenMinutes = configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 30;
        var refreshTokenDays = configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 14;
        if (accessTokenMinutes is < 1 or > 1_440)
        {
            throw new InvalidOperationException("Jwt:AccessTokenMinutes must be between 1 and 1440.");
        }

        if (refreshTokenDays is < 1 or > 90)
        {
            throw new InvalidOperationException("Jwt:RefreshTokenDays must be between 1 and 90.");
        }
    }

    private static void ValidateEmail(IConfiguration configuration)
    {
        var provider = configuration["Email:Provider"] ?? "Development";
        if (!provider.Equals("Development", StringComparison.OrdinalIgnoreCase) &&
            !provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Email:Provider '{provider}' is not supported.");
        }

        var appBaseUrl = configuration["Email:AppBaseUrl"] ?? "http://localhost:3000";
        if (!Uri.TryCreate(appBaseUrl, UriKind.Absolute, out var appBaseUri) ||
            (appBaseUri.Scheme != Uri.UriSchemeHttp && appBaseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Email:AppBaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        var verificationHours = configuration.GetValue<int?>("Email:VerificationTokenHours") ?? 24;
        var resetMinutes = configuration.GetValue<int?>("Email:PasswordResetTokenMinutes") ?? 60;
        var resendMinutes = configuration.GetValue<int?>("Email:ResendCooldownMinutes") ?? 5;
        if (verificationHours is < 1 or > 168)
        {
            throw new InvalidOperationException("Email:VerificationTokenHours must be between 1 and 168.");
        }

        if (resetMinutes is < 5 or > 1_440)
        {
            throw new InvalidOperationException("Email:PasswordResetTokenMinutes must be between 5 and 1440.");
        }

        if (resendMinutes is < 1 or > 1_440)
        {
            throw new InvalidOperationException("Email:ResendCooldownMinutes must be between 1 and 1440.");
        }

        if (!provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var port = configuration.GetValue<int?>("Email:Smtp:Port") ?? 587;
        var timeoutSeconds = configuration.GetValue<int?>("Email:Smtp:TimeoutSeconds") ?? 30;
        var retries = configuration.GetValue<int?>("Email:Smtp:MaxRetryAttempts") ?? 2;
        if (port is < 1 or > 65_535)
        {
            throw new InvalidOperationException("Email:Smtp:Port must be between 1 and 65535.");
        }

        if (timeoutSeconds is < 1 or > 300)
        {
            throw new InvalidOperationException("Email:Smtp:TimeoutSeconds must be between 1 and 300.");
        }

        if (retries is < 0 or > 10)
        {
            throw new InvalidOperationException("Email:Smtp:MaxRetryAttempts must be between 0 and 10.");
        }
    }

    private static void ValidateFileStorage(IConfiguration configuration)
    {
        var provider = configuration["FileStorage:Provider"] ?? "Local";
        if (!provider.Equals("Local", StringComparison.OrdinalIgnoreCase) &&
            !provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"FileStorage:Provider '{provider}' is not supported.");
        }

        var signedUrlMinutes = configuration.GetValue<int?>("FileStorage:SignedUrlMinutes") ?? 10;
        if (signedUrlMinutes is < 1 or > 1_440)
        {
            throw new InvalidOperationException("FileStorage:SignedUrlMinutes must be between 1 and 1440.");
        }
    }

    private static void ValidateRateLimiting(IConfiguration configuration)
    {
        ValidatePositiveRange(configuration, "RateLimiting:PermitLimit", 120, 1, 100_000);
        ValidatePositiveRange(configuration, "RateLimiting:WindowSeconds", 60, 1, 3_600);
        ValidatePositiveRange(configuration, "RateLimiting:AuthPermitLimit", 20, 1, 10_000);
        ValidatePositiveRange(configuration, "RateLimiting:AuthWindowSeconds", 60, 1, 3_600);
        ValidatePositiveRange(configuration, "RateLimiting:WebhookPermitLimit", 300, 1, 100_000);
        ValidatePositiveRange(configuration, "RateLimiting:WebhookWindowSeconds", 60, 1, 3_600);

        var queueLimit = configuration.GetValue<int?>("RateLimiting:QueueLimit") ?? 0;
        if (queueLimit is < 0 or > 10_000)
        {
            throw new InvalidOperationException("RateLimiting:QueueLimit must be between 0 and 10000.");
        }
    }

    private static void ValidateBackgroundJobs(IConfiguration configuration)
    {
        ValidatePositiveRange(configuration, "BackgroundJobs:IntervalMinutes", 15, 1, 1_440);
        ValidatePositiveRange(configuration, "BackgroundJobs:MaxRetryAttempts", 3, 1, 20);
        ValidatePositiveRange(configuration, "BackgroundJobs:BatchSize", 100, 1, 10_000);
        ValidatePositiveRange(configuration, "BackgroundJobs:EmailOutboxRetentionDays", 30, 1, 365);
        ValidatePositiveRange(configuration, "BackgroundJobs:FailedEmailOutboxRetentionDays", 90, 1, 730);
        ValidatePositiveRange(configuration, "BackgroundJobs:ExecutionRetentionDays", 90, 1, 730);
        ValidatePositiveRange(configuration, "BackgroundJobs:RefreshTokenRetentionDays", 30, 7, 365);

        var sentRetention = configuration.GetValue<int?>("BackgroundJobs:EmailOutboxRetentionDays") ?? 30;
        var failedRetention = configuration.GetValue<int?>("BackgroundJobs:FailedEmailOutboxRetentionDays") ?? 90;
        if (failedRetention < sentRetention)
        {
            throw new InvalidOperationException("BackgroundJobs:FailedEmailOutboxRetentionDays must be greater than or equal to EmailOutboxRetentionDays.");
        }
    }

    private static void ValidatePositiveRange(
        IConfiguration configuration,
        string key,
        int defaultValue,
        int minimum,
        int maximum)
    {
        var value = configuration.GetValue<int?>(key) ?? defaultValue;
        if (value < minimum || value > maximum)
        {
            throw new InvalidOperationException($"{key} must be between {minimum} and {maximum}.");
        }
    }

    private static void ValidateAI(IConfiguration configuration, IHostEnvironment environment)
    {
        var provider = configuration["AI:Provider"] ?? "Mock";
        if (!provider.Equals("Mock", StringComparison.OrdinalIgnoreCase) &&
            !provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"AI:Provider '{provider}' is not supported.");
        }

        var dailyQuota = configuration.GetValue<int?>("AI:DailyQuota") ?? 20;
        if (dailyQuota is < 1 or > 10_000)
        {
            throw new InvalidOperationException("AI:DailyQuota must be between 1 and 10000.");
        }

        if (environment.IsProduction() && provider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("AI:Provider must use Ollama before running in Production.");
        }

        if (!provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var baseUrl = configuration["AI:Ollama:BaseUrl"];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("AI:Ollama:BaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(configuration["AI:Ollama:Model"]))
        {
            throw new InvalidOperationException("AI:Ollama:Model is required when AI:Provider is Ollama.");
        }

        var timeoutSeconds = configuration.GetValue<int?>("AI:Ollama:TimeoutSeconds") ?? 120;
        if (timeoutSeconds is < 5 or > 600)
        {
            throw new InvalidOperationException("AI:Ollama:TimeoutSeconds must be between 5 and 600.");
        }
    }

    private static void ValidateObservability(IConfiguration configuration)
    {
        var headerName = configuration["Observability:CorrelationHeaderName"] ?? "X-Correlation-Id";
        if (headerName.Length is < 1 or > 64 ||
            !headerName.All(character => char.IsAsciiLetterOrDigit(character) || character == '-'))
        {
            throw new InvalidOperationException("Observability:CorrelationHeaderName must be a valid HTTP header name.");
        }

        var slowRequestThresholdMs = configuration.GetValue<int?>("Observability:SlowRequestThresholdMs") ?? 1000;
        if (slowRequestThresholdMs is < 1 or > 60_000)
        {
            throw new InvalidOperationException("Observability:SlowRequestThresholdMs must be between 1 and 60000.");
        }
    }

    private static void ValidateEmailOutbox(IConfiguration configuration, IHostEnvironment environment)
    {
        var enabled = !bool.TryParse(configuration["Email:Outbox:Enabled"], out var configuredEnabled) || configuredEnabled;
        if (environment.IsProduction() && !enabled)
        {
            throw new InvalidOperationException("Email:Outbox:Enabled must be true in Production.");
        }

        var pollSeconds = configuration.GetValue<int?>("Email:Outbox:PollSeconds") ?? 5;
        var batchSize = configuration.GetValue<int?>("Email:Outbox:BatchSize") ?? 50;
        var maxAttempts = configuration.GetValue<int?>("Email:Outbox:MaxAttempts") ?? 10;
        var leaseSeconds = configuration.GetValue<int?>("Email:Outbox:LeaseSeconds") ?? 600;
        if (pollSeconds is < 1 or > 300)
        {
            throw new InvalidOperationException("Email:Outbox:PollSeconds must be between 1 and 300.");
        }

        if (batchSize is < 1 or > 500)
        {
            throw new InvalidOperationException("Email:Outbox:BatchSize must be between 1 and 500.");
        }

        if (maxAttempts is < 1 or > 50)
        {
            throw new InvalidOperationException("Email:Outbox:MaxAttempts must be between 1 and 50.");
        }

        if (leaseSeconds is < 30 or > 3_600)
        {
            throw new InvalidOperationException("Email:Outbox:LeaseSeconds must be between 30 and 3600.");
        }

        var encryptionKey = configuration["Email:Outbox:EncryptionKey"];
        if (environment.IsProduction() &&
            (string.IsNullOrWhiteSpace(encryptionKey) ||
             encryptionKey.Length < 32 ||
             encryptionKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
             encryptionKey.Contains("DEV_ONLY", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Email:Outbox:EncryptionKey must be a production secret of at least 32 characters.");
        }
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
        var maxCvBytes = configuration.GetValue<long?>("FileStorage:MaxCvBytes") ?? 5 * 1024 * 1024;
        var maxRequestBodySize = configuration.GetValue<long?>("Security:MaxRequestBodySizeBytes") ?? 25 * 1024 * 1024;
        if (maxCvBytes is < 4 or > 100 * 1024 * 1024 || maxCvBytes > maxRequestBodySize)
        {
            throw new InvalidOperationException("FileStorage:MaxCvBytes must be between 4 bytes and Security:MaxRequestBodySizeBytes.");
        }

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
