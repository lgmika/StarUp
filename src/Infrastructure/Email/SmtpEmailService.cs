using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StartupConnect.Application.Email.Interfaces;
using StartupConnect.Application.Email.Models;

namespace StartupConnect.Infrastructure.Email;

public sealed class SmtpEmailService(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public Task SendEmailVerificationAsync(string email, string verificationUrl, CancellationToken cancellationToken)
    {
        return SendAsync(EmailTemplateRenderer.Verification(email, verificationUrl), cancellationToken);
    }

    public Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken)
    {
        return SendAsync(EmailTemplateRenderer.PasswordReset(email, resetUrl), cancellationToken);
    }

    public Task SendProjectInvitationAsync(string email, ProjectInvitationEmailModel model, CancellationToken cancellationToken)
    {
        return SendAsync(EmailTemplateRenderer.ProjectInvitation(email, model), cancellationToken);
    }

    public Task SendNotificationEmailAsync(string email, NotificationEmailModel model, CancellationToken cancellationToken)
    {
        return SendAsync(EmailTemplateRenderer.Notification(email, model), cancellationToken);
    }

    private async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Smtp.Host))
        {
            throw new InvalidOperationException("Email:Smtp:Host is required when Email:Provider is Smtp.");
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true
        };
        mail.To.Add(message.To);
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.TextBody, null, "text/plain"));

        var attempts = Math.Max(1, _options.Smtp.MaxRetryAttempts);
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var client = new SmtpClient(_options.Smtp.Host, _options.Smtp.Port)
                {
                    EnableSsl = _options.Smtp.EnableSsl,
                    Timeout = Math.Max(1, _options.Smtp.TimeoutSeconds) * 1000
                };

                if (!string.IsNullOrWhiteSpace(_options.Smtp.Username))
                {
                    client.Credentials = new NetworkCredential(_options.Smtp.Username, _options.Smtp.Password);
                }

                using var registration = cancellationToken.Register(client.SendAsyncCancel);
                await client.SendMailAsync(mail, cancellationToken);
                return;
            }
            catch (Exception exception) when (attempt < attempts && exception is SmtpException or TimeoutException or IOException)
            {
                logger.LogWarning(exception, "SMTP send attempt {Attempt}/{Attempts} failed for {Email}", attempt, attempts, message.To);
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
        }
    }
}
