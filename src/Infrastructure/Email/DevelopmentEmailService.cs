using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StartupConnect.Application.Email.Interfaces;
using StartupConnect.Application.Email.Models;

namespace StartupConnect.Infrastructure.Email;

public sealed class DevelopmentEmailService(
    IOptions<EmailOptions> options,
    ILogger<DevelopmentEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public Task SendEmailVerificationAsync(string email, string verificationUrl, CancellationToken cancellationToken)
    {
        return WriteAsync(EmailTemplateRenderer.Verification(email, verificationUrl), cancellationToken);
    }

    public Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken)
    {
        return WriteAsync(EmailTemplateRenderer.PasswordReset(email, resetUrl), cancellationToken);
    }

    public Task SendProjectInvitationAsync(string email, ProjectInvitationEmailModel model, CancellationToken cancellationToken)
    {
        return WriteAsync(EmailTemplateRenderer.ProjectInvitation(email, model), cancellationToken);
    }

    public Task SendNotificationEmailAsync(string email, NotificationEmailModel model, CancellationToken cancellationToken)
    {
        return WriteAsync(EmailTemplateRenderer.Notification(email, model), cancellationToken);
    }

    private async Task WriteAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Development email queued for {Email}: {Subject}", message.To, message.Subject);

        Directory.CreateDirectory(_options.DevLogDirectory);
        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.eml";
        var path = Path.Combine(_options.DevLogDirectory, fileName);

        var content = $"""
                      To: {message.To}
                      From: {_options.FromName} <{_options.FromEmail}>
                      Subject: {message.Subject}

                      TEXT:
                      {message.TextBody}

                      HTML:
                      {message.HtmlBody}
                      """;

        await File.WriteAllTextAsync(path, content, cancellationToken);
        logger.LogInformation("Development email written to {Path}", path);
    }
}
