using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using StartupConnect.Api.Observability;
using StartupConnect.Api.Middlewares;
using StartupConnect.Api.Security;
using StartupConnect.Application.Admin.Dtos;
using StartupConnect.Application.Files.Dtos;
using StartupConnect.Application.Reports.Dtos;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Email;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StartupConnect.Tests;

public sealed class ProductionReadinessTests
{
    [Fact]
    public void Json_Enums_Should_Reject_Undefined_Integer_Values()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProjectStatus>("999", options));
        Assert.Equal(ProjectStatus.Published, JsonSerializer.Deserialize<ProjectStatus>("\"Published\"", options));
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Reject_Invalid_Forwarded_Network()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Security:KnownNetworks:0"] = "172.29.0.0/not-a-prefix"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Development")));

        Assert.Contains("KnownNetworks", exception.Message);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://app.startupconnect.example/path")]
    [InlineData("https://app.startupconnect.example?tenant=1")]
    [InlineData("https://app.startupconnect.example/")]
    public void SecurityConfigurationValidator_Should_Reject_Invalid_Cors_Origins(string origin)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = origin
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Development")));

        Assert.Contains("Cors:AllowedOrigins", exception.Message);
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Require_Jwt_Issuer_And_Audience()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = ""
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Development")));

        Assert.Contains("Jwt:Issuer", exception.Message);
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Require_Secure_Host_Prefixed_Cookie()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Security:RefreshTokenCookie:Enabled"] = "true",
            ["Security:RefreshTokenCookie:Name"] = "__Host-startupconnect-refresh",
            ["Security:RefreshTokenCookie:Secure"] = "false"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Development")));

        Assert.Contains("__Host-", exception.Message);
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Reject_SameSite_None_In_Production()
    {
        var configuration = CreateProductionReadyConfiguration(new Dictionary<string, string?>
        {
            ["Security:RefreshTokenCookie:Enabled"] = "true",
            ["Security:RefreshTokenCookie:Secure"] = "true",
            ["Security:RefreshTokenCookie:SameSite"] = "None"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Production")));

        Assert.Contains("CSRF", exception.Message);
    }

    [Theory]
    [InlineData("Jwt:AccessTokenMinutes", "0")]
    [InlineData("Jwt:RefreshTokenDays", "91")]
    [InlineData("Email:Outbox:LeaseSeconds", "10")]
    [InlineData("BackgroundJobs:EmailOutboxRetentionDays", "0")]
    [InlineData("BackgroundJobs:ExecutionRetentionDays", "0")]
    [InlineData("BackgroundJobs:RefreshTokenRetentionDays", "1")]
    public void SecurityConfigurationValidator_Should_Reject_Unsafe_Operational_Ranges(string key, string value)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?> { [key] = value });

        Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Development")));
    }

    [Theory]
    [InlineData("Email:Provider", "Unknown")]
    [InlineData("FileStorage:Provider", "Unknown")]
    public void SecurityConfigurationValidator_Should_Reject_Unknown_Infrastructure_Providers(string key, string value)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?> { [key] = value });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Development")));

        Assert.Contains("not supported", exception.Message);
    }
    [Fact]
    public void AdminSettingDto_Should_Expose_Frontend_Config_Metadata()
    {
        var dto = new AdminSettingDto(
            "AI.DailyQuota",
            "AI",
            "20",
            "number",
            false,
            DateTimeOffset.UtcNow);

        Assert.Equal("AI", dto.Group);
        Assert.Equal("number", dto.Type);
        Assert.False(dto.IsReadonly);
    }

    [Fact]
    public void NotificationType_Should_Cover_Core_Product_Domains()
    {
        Assert.True(Enum.IsDefined(NotificationType.Application));
        Assert.True(Enum.IsDefined(NotificationType.InvestorInterest));
        Assert.True(Enum.IsDefined(NotificationType.Chat));
        Assert.True(Enum.IsDefined(NotificationType.Report));
        Assert.True(Enum.IsDefined(NotificationType.NDA));
        Assert.True(Enum.IsDefined(NotificationType.Interview));
        Assert.True(Enum.IsDefined(NotificationType.Billing));
    }

    [Fact]
    public void FileMetadataDto_Should_Not_Expose_Private_Storage_Path()
    {
        var dto = new FileMetadataDto(
            Guid.NewGuid(),
            "pitch.pdf",
            "application/pdf",
            1024,
            false,
            DateTimeOffset.UtcNow,
            null);

        Assert.Equal("pitch.pdf", dto.OriginalFileName);
        Assert.DoesNotContain("StoragePath", dto.ToString());
    }

    [Fact]
    public void ReportTargetContextDto_Should_Represent_Unreportable_Target()
    {
        var dto = new ReportTargetContextDto(
            "User",
            Guid.NewGuid(),
            true,
            false,
            "Self",
            "self@example.com",
            "You cannot report yourself");

        Assert.False(dto.CanReport);
        Assert.NotNull(dto.Reason);
    }

    [Theory]
    [InlineData("None", SameSiteMode.None)]
    [InlineData("Lax", SameSiteMode.Lax)]
    [InlineData("Strict", SameSiteMode.Strict)]
    [InlineData("unexpected", SameSiteMode.Strict)]
    public void AuthCookieHelper_Should_Parse_SameSite_Defensively(string configuredValue, SameSiteMode expected)
    {
        Assert.Equal(expected, AuthCookieHelper.ParseSameSite(configuredValue));
    }

    [Fact]
    public void AuthCookieHelper_Should_Hide_Refresh_Token_From_Response_When_Cookie_Mode_Is_Enabled()
    {
        var context = new DefaultHttpContext();
        var options = new StartupConnectSecurityOptions
        {
            RefreshTokenCookie = new RefreshTokenCookieSettings
            {
                Enabled = true,
                Secure = true,
                SameSite = "Strict"
            }
        };
        var response = new StartupConnect.Application.Auth.Dtos.AuthResponse(
            "access-token",
            DateTimeOffset.UtcNow.AddMinutes(30),
            "secret-refresh-token",
            DateTimeOffset.UtcNow.AddDays(14),
            new StartupConnect.Application.Auth.Dtos.AuthUserDto(
                Guid.NewGuid(),
                "user@example.com",
                "User",
                true,
                []));

        var clientResponse = AuthCookieHelper.PrepareClientResponse(context.Response, response, options);

        Assert.Empty(clientResponse.RefreshToken);
        Assert.Contains("secret-refresh-token", context.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Reject_Dev_Secrets_Outside_Development()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "DEV_ONLY_StartupConnect_JwtSigningKey_AtLeast32Chars_ReplaceInProduction",
                ["Jwt:Issuer"] = "StartupConnect",
                ["Jwt:Audience"] = "StartupConnect.Client",
                ["AllowedHosts"] = "api.startupconnect.example",
                ["Cors:AllowedOrigins:0"] = "https://app.startupconnect.example"
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Production")));
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Reject_Unverified_Email_Sender_Domain_In_Production()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "prod_signing_key_that_is_long_enough_for_tests",
                ["Jwt:Issuer"] = "StartupConnect",
                ["Jwt:Audience"] = "StartupConnect.Client",
                ["Security:KnownProxies:0"] = "172.29.0.10",
                ["AllowedHosts"] = "api.startupconnect.example",
                ["Cors:AllowedOrigins:0"] = "https://app.startupconnect.example",
                ["Payments:Provider"] = "Stripe",
                ["Payments:ApiKey"] = "sk_test_valid_for_validator",
                ["Payments:WebhookSecret"] = "whsec_valid_for_validator",
                ["Payments:CheckoutBaseUrl"] = "https://app.startupconnect.example/billing/checkout",
                ["AI:Provider"] = "Ollama",
                ["AI:Ollama:BaseUrl"] = "http://ollama:11434",
                ["AI:Ollama:Model"] = "llama3.1",
                ["Email:Provider"] = "Smtp",
                ["Email:FromEmail"] = "no-reply@wrong.example",
                ["Email:AppBaseUrl"] = "https://app.startupconnect.example",
                ["Email:RequireVerifiedSenderDomain"] = "true",
                ["Email:VerifiedSenderDomain"] = "startupconnect.example",
                ["Email:Smtp:Host"] = "smtp.example.com",
                ["Email:Smtp:Username"] = "smtp-user",
                ["Email:Smtp:Password"] = "smtp-password",
                ["Email:Outbox:EncryptionKey"] = "production_email_outbox_encryption_key_for_tests"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Production")));

        Assert.Contains("VerifiedSenderDomain", exception.Message);
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Reject_Local_File_Storage_In_Production()
    {
        var configuration = CreateProductionReadyConfiguration(
            new Dictionary<string, string?>
            {
                ["FileStorage:Provider"] = "Local"
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Production")));

        Assert.Contains("FileStorage:Provider", exception.Message);
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Require_S3_Bucket_When_S3_Is_Selected()
    {
        var configuration = CreateProductionReadyConfiguration(
            new Dictionary<string, string?>
            {
                ["FileStorage:Provider"] = "S3",
                ["FileStorage:S3:BucketName"] = "",
                ["FileStorage:S3:Region"] = "us-east-1"
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Production")));

        Assert.Contains("BucketName", exception.Message);
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Reject_Cv_Limit_Above_Request_Limit()
    {
        var configuration = CreateProductionReadyConfiguration(
            new Dictionary<string, string?>
            {
                ["Security:MaxRequestBodySizeBytes"] = "1048576",
                ["FileStorage:MaxCvBytes"] = "2097152"
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Production")));

        Assert.Contains("MaxCvBytes", exception.Message);
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Require_Rate_Limiting_In_Production()
    {
        var configuration = CreateProductionReadyConfiguration(
            new Dictionary<string, string?>
            {
                ["RateLimiting:Enabled"] = "false"
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Production")));

        Assert.Contains("RateLimiting:Enabled", exception.Message);
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Require_Security_Headers_In_Production()
    {
        var configuration = CreateProductionReadyConfiguration(
            new Dictionary<string, string?>
            {
                ["Security:EnableSecurityHeaders"] = "false"
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Production")));

        Assert.Contains("EnableSecurityHeaders", exception.Message);
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Reject_Insecure_Refresh_Cookie_Outside_Development()
    {
        var configuration = CreateProductionReadyConfiguration(
            new Dictionary<string, string?>
            {
                ["Security:RefreshTokenCookie:Enabled"] = "true",
                ["Security:RefreshTokenCookie:Secure"] = "false",
                ["Security:RefreshTokenCookie:SameSite"] = "Lax"
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Staging")));

        Assert.Contains("RefreshTokenCookie:Secure", exception.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("104857601")]
    public void SecurityConfigurationValidator_Should_Reject_Unsafe_Request_Body_Limits(string value)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "DEV_ONLY_StartupConnect_JwtSigningKey_AtLeast32Chars_ReplaceInProduction",
                ["Jwt:Issuer"] = "StartupConnect",
                ["Jwt:Audience"] = "StartupConnect.Client",
                ["Security:MaxRequestBodySizeBytes"] = value
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Development")));

        Assert.Contains("MaxRequestBodySizeBytes", exception.Message);
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Reject_Mock_AI_In_Production()
    {
        var configuration = CreateProductionReadyConfiguration(
            new Dictionary<string, string?>
            {
                ["AI:Provider"] = "Mock"
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Production")));

        Assert.Contains("AI:Provider", exception.Message);
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Reject_Unknown_AI_Provider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "DEV_ONLY_StartupConnect_JwtSigningKey_AtLeast32Chars_ReplaceInProduction",
                ["Jwt:Issuer"] = "StartupConnect",
                ["Jwt:Audience"] = "StartupConnect.Client",
                ["AI:Provider"] = "Unknown"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Development")));

        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Reject_Unknown_Payment_Provider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "DEV_ONLY_StartupConnect_JwtSigningKey_AtLeast32Chars_ReplaceInProduction",
                ["Jwt:Issuer"] = "StartupConnect",
                ["Jwt:Audience"] = "StartupConnect.Client",
                ["Payments:Provider"] = "Unknown"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Development")));

        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public void OperationsOptions_Should_Default_To_Production_Friendly_Values()
    {
        var observability = new ObservabilityOptions();
        var rateLimits = new ApiRateLimitOptions();

        Assert.True(observability.EnableRequestLogging);
        Assert.Equal("X-Correlation-Id", observability.CorrelationHeaderName);
        Assert.True(rateLimits.Enabled);
        Assert.True(rateLimits.AuthPermitLimit < rateLimits.PermitLimit);
        Assert.True(new EmailOutboxOptions().Enabled);
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Require_Email_Outbox_In_Production()
    {
        var configuration = CreateProductionReadyConfiguration(
            new Dictionary<string, string?>
            {
                ["Email:Outbox:Enabled"] = "false"
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Production")));

        Assert.Contains("Email:Outbox:Enabled", exception.Message);
    }

    [Theory]
    [InlineData("request-123", "request-123")]
    [InlineData("request_123.trace", "request_123.trace")]
    [InlineData("bad\r\nheader", "server-trace")]
    public void RequestLoggingMiddleware_Should_Normalize_Client_Correlation_Id(string value, string expected)
    {
        Assert.Equal(expected, RequestLoggingMiddleware.NormalizeCorrelationId(value, "server-trace"));
    }

    [Fact]
    public void SecurityConfigurationValidator_Should_Reject_Invalid_Correlation_Header_Name()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "DEV_ONLY_StartupConnect_JwtSigningKey_AtLeast32Chars_ReplaceInProduction",
                ["Jwt:Issuer"] = "StartupConnect",
                ["Jwt:Audience"] = "StartupConnect.Client",
                ["Observability:CorrelationHeaderName"] = "Bad Header"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SecurityConfigurationValidator.Validate(configuration, new TestHostEnvironment("Development")));

        Assert.Contains("CorrelationHeaderName", exception.Message);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "StartupConnect.Tests";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IConfiguration BuildConfiguration(IReadOnlyDictionary<string, string?> overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "DEV_ONLY_StartupConnect_JwtSigningKey_AtLeast32Chars_ReplaceInProduction",
            ["Jwt:Issuer"] = "StartupConnect",
            ["Jwt:Audience"] = "StartupConnect.Client",
            ["Security:UseForwardedHeaders"] = "false"
        };

        foreach (var item in overrides)
        {
            values[item.Key] = item.Value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static IConfiguration CreateProductionReadyConfiguration(IReadOnlyDictionary<string, string?> overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "prod_signing_key_that_is_long_enough_for_tests",
            ["Jwt:Issuer"] = "StartupConnect",
            ["Jwt:Audience"] = "StartupConnect.Client",
            ["Security:KnownProxies:0"] = "172.29.0.10",
            ["AllowedHosts"] = "api.startupconnect.example",
            ["Cors:AllowedOrigins:0"] = "https://app.startupconnect.example",
            ["Payments:Provider"] = "Stripe",
            ["Payments:ApiKey"] = "sk_test_valid_for_validator",
            ["Payments:WebhookSecret"] = "whsec_valid_for_validator",
            ["Payments:CheckoutBaseUrl"] = "https://app.startupconnect.example/billing/checkout",
            ["AI:Provider"] = "Ollama",
            ["AI:DailyQuota"] = "20",
            ["AI:Ollama:BaseUrl"] = "http://ollama:11434",
            ["AI:Ollama:Model"] = "llama3.1",
            ["AI:Ollama:TimeoutSeconds"] = "120",
            ["Email:Provider"] = "Smtp",
            ["Email:FromEmail"] = "no-reply@startupconnect.example",
            ["Email:AppBaseUrl"] = "https://app.startupconnect.example",
            ["Email:RequireVerifiedSenderDomain"] = "true",
            ["Email:VerifiedSenderDomain"] = "startupconnect.example",
            ["Email:Smtp:Host"] = "smtp.example.com",
            ["Email:Smtp:Username"] = "smtp-user",
            ["Email:Smtp:Password"] = "smtp-password",
            ["Email:Outbox:EncryptionKey"] = "production_email_outbox_encryption_key_for_tests",
            ["FileStorage:Provider"] = "S3",
            ["FileStorage:S3:BucketName"] = "startupconnect-files",
            ["FileStorage:S3:Region"] = "us-east-1"
        };

        foreach (var item in overrides)
        {
            values[item.Key] = item.Value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
