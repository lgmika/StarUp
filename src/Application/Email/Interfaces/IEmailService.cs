using StartupConnect.Application.Email.Models;

namespace StartupConnect.Application.Email.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(
        string email,
        string verificationUrl,
        CancellationToken cancellationToken);

    Task SendPasswordResetAsync(
        string email,
        string resetUrl,
        CancellationToken cancellationToken);

    Task SendProjectInvitationAsync(
        string email,
        ProjectInvitationEmailModel model,
        CancellationToken cancellationToken);

    Task SendNotificationEmailAsync(
        string email,
        NotificationEmailModel model,
        CancellationToken cancellationToken);
}
