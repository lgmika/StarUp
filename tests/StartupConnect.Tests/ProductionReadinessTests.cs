using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using StartupConnect.Api.Observability;
using StartupConnect.Api.Security;
using StartupConnect.Application.Admin.Dtos;
using StartupConnect.Application.Files.Dtos;
using StartupConnect.Application.Reports.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class ProductionReadinessTests
{
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
    public void SecurityConfigurationValidator_Should_Reject_Dev_Secrets_Outside_Development()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "DEV_ONLY_StartupConnect_JwtSigningKey_AtLeast32Chars_ReplaceInProduction",
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
                ["AllowedHosts"] = "api.startupconnect.example",
                ["Cors:AllowedOrigins:0"] = "https://app.startupconnect.example",
                ["Payments:Provider"] = "Stripe",
                ["Payments:ApiKey"] = "sk_test_valid_for_validator",
                ["Payments:WebhookSecret"] = "whsec_valid_for_validator",
                ["Email:Provider"] = "Smtp",
                ["Email:FromEmail"] = "no-reply@wrong.example",
                ["Email:AppBaseUrl"] = "https://app.startupconnect.example",
                ["Email:RequireVerifiedSenderDomain"] = "true",
                ["Email:VerifiedSenderDomain"] = "startupconnect.example",
                ["Email:Smtp:Host"] = "smtp.example.com",
                ["Email:Smtp:Username"] = "smtp-user",
                ["Email:Smtp:Password"] = "smtp-password"
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
    public void OperationsOptions_Should_Default_To_Production_Friendly_Values()
    {
        var observability = new ObservabilityOptions();
        var rateLimits = new ApiRateLimitOptions();

        Assert.True(observability.EnableRequestLogging);
        Assert.Equal("X-Correlation-Id", observability.CorrelationHeaderName);
        Assert.True(rateLimits.Enabled);
        Assert.True(rateLimits.AuthPermitLimit < rateLimits.PermitLimit);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "StartupConnect.Tests";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IConfiguration CreateProductionReadyConfiguration(IReadOnlyDictionary<string, string?> overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "prod_signing_key_that_is_long_enough_for_tests",
            ["AllowedHosts"] = "api.startupconnect.example",
            ["Cors:AllowedOrigins:0"] = "https://app.startupconnect.example",
            ["Payments:Provider"] = "Stripe",
            ["Payments:ApiKey"] = "sk_test_valid_for_validator",
            ["Payments:WebhookSecret"] = "whsec_valid_for_validator",
            ["Email:Provider"] = "Smtp",
            ["Email:FromEmail"] = "no-reply@startupconnect.example",
            ["Email:AppBaseUrl"] = "https://app.startupconnect.example",
            ["Email:RequireVerifiedSenderDomain"] = "true",
            ["Email:VerifiedSenderDomain"] = "startupconnect.example",
            ["Email:Smtp:Host"] = "smtp.example.com",
            ["Email:Smtp:Username"] = "smtp-user",
            ["Email:Smtp:Password"] = "smtp-password",
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
