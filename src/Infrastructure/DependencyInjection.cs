using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StartupConnect.Application.Activities.Interfaces;
using StartupConnect.Application.Admin.Interfaces;
using StartupConnect.Application.AI.Interfaces;
using StartupConnect.Application.Applications.Interfaces;
using StartupConnect.Application.Auth.Interfaces;
using StartupConnect.Application.BackgroundJobs.Interfaces;
using StartupConnect.Application.Chat.Interfaces;
using StartupConnect.Application.Dashboards.Interfaces;
using StartupConnect.Application.Email.Interfaces;
using StartupConnect.Application.Files.Interfaces;
using StartupConnect.Application.Investors.Interfaces;
using StartupConnect.Application.Interviews.Interfaces;
using StartupConnect.Application.Moderation.Interfaces;
using StartupConnect.Application.Nda.Interfaces;
using StartupConnect.Application.Notifications.Interfaces;
using StartupConnect.Application.Profiles.Interfaces;
using StartupConnect.Application.ProjectTeams.Interfaces;
using StartupConnect.Application.Projects.Interfaces;
using StartupConnect.Application.Realtime.Interfaces;
using StartupConnect.Application.Recommendations.Interfaces;
using StartupConnect.Application.Reports.Interfaces;
using StartupConnect.Application.Search.Interfaces;
using StartupConnect.Application.Subscriptions.Interfaces;
using StartupConnect.Infrastructure.Activities;
using StartupConnect.Infrastructure.AI;
using StartupConnect.Infrastructure.Admin;
using StartupConnect.Infrastructure.Applications;
using StartupConnect.Infrastructure.Auth;
using StartupConnect.Infrastructure.BackgroundJobs;
using StartupConnect.Infrastructure.Chat;
using StartupConnect.Infrastructure.Dashboards;
using StartupConnect.Infrastructure.Email;
using StartupConnect.Infrastructure.Files;
using StartupConnect.Infrastructure.Investors;
using StartupConnect.Infrastructure.Interviews;
using StartupConnect.Infrastructure.Moderation;
using StartupConnect.Infrastructure.Nda;
using StartupConnect.Infrastructure.Notifications;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Infrastructure.Profiles;
using StartupConnect.Infrastructure.ProjectTeams;
using StartupConnect.Infrastructure.Projects;
using StartupConnect.Infrastructure.Realtime;
using StartupConnect.Infrastructure.Recommendations;
using StartupConnect.Infrastructure.Reports;
using StartupConnect.Infrastructure.Search;
using StartupConnect.Infrastructure.Subscriptions;

namespace StartupConnect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = configuration["Jwt:Issuer"] ?? string.Empty;
            options.Audience = configuration["Jwt:Audience"] ?? string.Empty;
            options.SigningKey = configuration["Jwt:SigningKey"] ?? string.Empty;
            options.AccessTokenMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var accessTokenMinutes)
                ? accessTokenMinutes
                : 30;
            options.RefreshTokenDays = int.TryParse(configuration["Jwt:RefreshTokenDays"], out var refreshTokenDays)
                ? refreshTokenDays
                : 14;
        });
        services.AddScoped<PasswordHasher>();
        services.AddScoped<SecureTokenGenerator>();
        services.AddScoped<JwtTokenService>();
        services.Configure<EmailOptions>(options =>
        {
            options.Provider = configuration["Email:Provider"] ?? "Development";
            options.FromEmail = configuration["Email:FromEmail"] ?? "no-reply@startupconnect.local";
            options.FromName = configuration["Email:FromName"] ?? "StartupConnect";
            options.AppBaseUrl = configuration["Email:AppBaseUrl"] ?? "http://localhost:3000";
            options.VerifiedSenderDomain = configuration["Email:VerifiedSenderDomain"] ?? string.Empty;
            options.RequireVerifiedSenderDomain = bool.TryParse(configuration["Email:RequireVerifiedSenderDomain"], out var requireVerifiedDomain) && requireVerifiedDomain;
            options.DevLogDirectory = configuration["Email:DevLogDirectory"] ?? "App_Data/emails";
            options.VerificationTokenHours = int.TryParse(configuration["Email:VerificationTokenHours"], out var verificationHours)
                ? verificationHours
                : 24;
            options.PasswordResetTokenMinutes = int.TryParse(configuration["Email:PasswordResetTokenMinutes"], out var resetMinutes)
                ? resetMinutes
                : 60;
            options.ResendCooldownMinutes = int.TryParse(configuration["Email:ResendCooldownMinutes"], out var resendMinutes)
                ? resendMinutes
                : 5;
            options.Smtp.Host = configuration["Email:Smtp:Host"] ?? string.Empty;
            options.Smtp.Port = int.TryParse(configuration["Email:Smtp:Port"], out var smtpPort) ? smtpPort : 587;
            options.Smtp.EnableSsl = bool.TryParse(configuration["Email:Smtp:EnableSsl"], out var smtpSsl) ? smtpSsl : true;
            options.Smtp.Username = configuration["Email:Smtp:Username"] ?? string.Empty;
            options.Smtp.Password = configuration["Email:Smtp:Password"] ?? string.Empty;
            options.Smtp.TimeoutSeconds = int.TryParse(configuration["Email:Smtp:TimeoutSeconds"], out var smtpTimeout) ? smtpTimeout : 30;
            options.Smtp.MaxRetryAttempts = int.TryParse(configuration["Email:Smtp:MaxRetryAttempts"], out var smtpRetries) ? smtpRetries : 2;
        });
        services.AddScoped<IEmailService>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailOptions>>().Value;
            return options.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase)
                ? serviceProvider.GetRequiredService<SmtpEmailService>()
                : serviceProvider.GetRequiredService<DevelopmentEmailService>();
        });
        services.AddScoped<DevelopmentEmailService>();
        services.AddScoped<SmtpEmailService>();
        services.Configure<EmailOutboxOptions>(options =>
        {
            options.Enabled = !bool.TryParse(configuration["Email:Outbox:Enabled"], out var enabled) || enabled;
            options.PollSeconds = int.TryParse(configuration["Email:Outbox:PollSeconds"], out var pollSeconds) ? pollSeconds : 5;
            options.BatchSize = int.TryParse(configuration["Email:Outbox:BatchSize"], out var batchSize) ? batchSize : 50;
            options.MaxAttempts = int.TryParse(configuration["Email:Outbox:MaxAttempts"], out var maxAttempts) ? maxAttempts : 10;
            options.LeaseSeconds = int.TryParse(configuration["Email:Outbox:LeaseSeconds"], out var leaseSeconds) ? leaseSeconds : 600;
            options.EncryptionKey = configuration["Email:Outbox:EncryptionKey"] ??
                configuration["Jwt:SigningKey"] ??
                string.Empty;
        });
        services.AddSingleton<EmailOutboxProtector>();
        services.AddScoped<EmailOutboxDispatcher>();
        services.AddHostedService<EmailOutboxWorker>();
        services.Configure<FileStorageOptions>(options =>
        {
            options.Provider = configuration["FileStorage:Provider"] ?? "Local";
            options.LocalRootPath = configuration["FileStorage:LocalRootPath"] ?? "storage/private";
            options.PublicBaseUrl = configuration["FileStorage:PublicBaseUrl"] ?? "http://localhost:8080";
            options.SignedUrlMinutes = int.TryParse(configuration["FileStorage:SignedUrlMinutes"], out var signedUrlMinutes)
                ? signedUrlMinutes
                : 10;
            options.MaxCvBytes = long.TryParse(configuration["FileStorage:MaxCvBytes"], out var maxCvBytes)
                ? maxCvBytes
                : 5 * 1024 * 1024;
            options.SigningKey = configuration["FileStorage:SigningKey"] ??
                configuration["Jwt:SigningKey"] ??
                string.Empty;
            options.S3.BucketName = configuration["FileStorage:S3:BucketName"] ?? string.Empty;
            options.S3.Region = configuration["FileStorage:S3:Region"] ?? "us-east-1";
            options.S3.ServiceUrl = configuration["FileStorage:S3:ServiceUrl"] ?? string.Empty;
            options.S3.AccessKeyId = configuration["FileStorage:S3:AccessKeyId"] ?? string.Empty;
            options.S3.SecretAccessKey = configuration["FileStorage:S3:SecretAccessKey"] ?? string.Empty;
            options.S3.KeyPrefix = configuration["FileStorage:S3:KeyPrefix"] ?? "startupconnect";
            options.S3.ForcePathStyle = bool.TryParse(configuration["FileStorage:S3:ForcePathStyle"], out var forcePathStyle) && forcePathStyle;
            options.S3.UseServerSideEncryption = !bool.TryParse(configuration["FileStorage:S3:UseServerSideEncryption"], out var useSse) || useSse;
        });
        services.AddScoped<LocalFileStorageService>();
        services.AddScoped<S3FileStorageService>();
        services.AddScoped<IFileStorageService>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileStorageOptions>>().Value;
            if (options.Provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                return serviceProvider.GetRequiredService<LocalFileStorageService>();
            }

            if (options.Provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
            {
                return serviceProvider.GetRequiredService<S3FileStorageService>();
            }

            throw new InvalidOperationException($"File storage provider '{options.Provider}' is not configured in this build.");
        });
        services.AddScoped<IFileService, FileService>();
        services.Configure<PaymentOptions>(options =>
        {
            options.Provider = configuration["Payments:Provider"] ?? "Mock";
            options.WebhookSecret = configuration["Payments:WebhookSecret"] ?? "DEV_ONLY_PaymentWebhookSecret_ReplaceInProduction";
            options.CheckoutBaseUrl = configuration["Payments:CheckoutBaseUrl"] ?? "http://localhost:3000/billing/checkout";
            options.ProductionCheckoutBaseUrl = configuration["Payments:ProductionCheckoutBaseUrl"] ?? string.Empty;
            options.ApiKey = configuration["Payments:ApiKey"] ?? string.Empty;
        });
        services.AddScoped<MockPaymentProvider>();
        services.AddScoped<StripePaymentProvider>();
        services.AddScoped<IPaymentProvider>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PaymentOptions>>().Value;
            if (options.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
            {
                return serviceProvider.GetRequiredService<MockPaymentProvider>();
            }

            if (options.Provider.Equals("Stripe", StringComparison.OrdinalIgnoreCase))
            {
                return serviceProvider.GetRequiredService<StripePaymentProvider>();
            }

            throw new InvalidOperationException($"Payment provider '{options.Provider}' is not configured in this build.");
        });
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.Configure<AIOptions>(options =>
        {
            options.Provider = configuration["AI:Provider"] ?? "Mock";
            options.DailyQuota = int.TryParse(configuration["AI:DailyQuota"], out var dailyQuota) ? dailyQuota : 20;
            options.Ollama.BaseUrl = configuration["AI:Ollama:BaseUrl"] ?? "http://localhost:11434";
            options.Ollama.Model = configuration["AI:Ollama:Model"] ?? "llama3.1";
            options.Ollama.TimeoutSeconds = int.TryParse(configuration["AI:Ollama:TimeoutSeconds"], out var ollamaTimeout)
                ? ollamaTimeout
                : 120;
        });
        services.AddScoped<MockAIProvider>();
        services.AddSingleton<OllamaHttpClient>();
        services.AddScoped<OllamaAIProvider>();
        services.AddScoped<IAIProvider>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AIOptions>>().Value;
            if (options.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
            {
                return serviceProvider.GetRequiredService<MockAIProvider>();
            }

            if (options.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                return serviceProvider.GetRequiredService<OllamaAIProvider>();
            }

            throw new InvalidOperationException($"AI provider '{options.Provider}' is not configured in this build.");
        });
        services.AddScoped<IAIService, AIService>();
        services.AddScoped<IModeratorService, ModeratorService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IInvestorService, InvestorService>();
        services.AddScoped<INdaService, NdaService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<ISystemSettingReader, SystemSettingReader>();
        services.AddScoped<IProjectTeamService, ProjectTeamService>();
        services.AddScoped<IInterviewService, InterviewService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IRealtimeNotifier, NullRealtimeNotifier>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.Configure<BackgroundJobOptions>(options =>
        {
            options.Enabled = bool.TryParse(configuration["BackgroundJobs:Enabled"], out var enabled) ? enabled : true;
            options.IntervalMinutes = int.TryParse(configuration["BackgroundJobs:IntervalMinutes"], out var intervalMinutes)
                ? intervalMinutes
                : 15;
            options.MaxRetryAttempts = int.TryParse(configuration["BackgroundJobs:MaxRetryAttempts"], out var maxRetryAttempts)
                ? maxRetryAttempts
                : 3;
            options.BatchSize = int.TryParse(configuration["BackgroundJobs:BatchSize"], out var batchSize)
                ? batchSize
                : 100;
            options.EmailOutboxRetentionDays = int.TryParse(configuration["BackgroundJobs:EmailOutboxRetentionDays"], out var outboxRetentionDays)
                ? outboxRetentionDays
                : 30;
            options.FailedEmailOutboxRetentionDays = int.TryParse(configuration["BackgroundJobs:FailedEmailOutboxRetentionDays"], out var failedOutboxRetentionDays)
                ? failedOutboxRetentionDays
                : 90;
            options.ExecutionRetentionDays = int.TryParse(configuration["BackgroundJobs:ExecutionRetentionDays"], out var executionRetentionDays)
                ? executionRetentionDays
                : 90;
            options.RefreshTokenRetentionDays = int.TryParse(configuration["BackgroundJobs:RefreshTokenRetentionDays"], out var refreshTokenRetentionDays)
                ? refreshTokenRetentionDays
                : 30;
            options.MaintenanceLockKey = long.TryParse(configuration["BackgroundJobs:MaintenanceLockKey"], out var lockKey)
                ? lockKey
                : 25061225;
        });
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddHostedService<StartupConnectBackgroundWorker>();

        return services;
    }
}
