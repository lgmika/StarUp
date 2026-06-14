namespace StartupConnect.Infrastructure.Email;

public sealed class EmailOptions
{
    public string Provider { get; set; } = "Development";

    public string FromEmail { get; set; } = "no-reply@startupconnect.local";

    public string FromName { get; set; } = "StartupConnect";

    public string AppBaseUrl { get; set; } = "http://localhost:3000";

    public string VerifiedSenderDomain { get; set; } = string.Empty;

    public bool RequireVerifiedSenderDomain { get; set; }

    public string DevLogDirectory { get; set; } = "App_Data/emails";

    public int VerificationTokenHours { get; set; } = 24;

    public int PasswordResetTokenMinutes { get; set; } = 60;

    public int ResendCooldownMinutes { get; set; } = 5;

    public SmtpEmailOptions Smtp { get; set; } = new();
}

public sealed class SmtpEmailOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxRetryAttempts { get; set; } = 2;
}
