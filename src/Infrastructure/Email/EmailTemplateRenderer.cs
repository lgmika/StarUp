using System.Net;
using StartupConnect.Application.Email.Models;

namespace StartupConnect.Infrastructure.Email;

internal static class EmailTemplateRenderer
{
    public static EmailMessage Verification(string email, string verificationUrl)
    {
        const string title = "Verify your StartupConnect email";
        var text = $"Welcome to StartupConnect. Verify your email: {verificationUrl}";
        return new EmailMessage(email, title, Layout(title, "Confirm your account", "Please verify your email address to unlock StartupConnect collaboration features.", verificationUrl, "Verify email"), text);
    }

    public static EmailMessage PasswordReset(string email, string resetUrl)
    {
        const string title = "Reset your StartupConnect password";
        var text = $"Reset your StartupConnect password: {resetUrl}";
        return new EmailMessage(email, title, Layout(title, "Reset password", "Use the secure link below to set a new password. The link expires soon and can be used once.", resetUrl, "Reset password"), text);
    }

    public static EmailMessage ProjectInvitation(string email, ProjectInvitationEmailModel model)
    {
        var subject = $"Invitation to join {model.ProjectName}";
        var body = $"{model.InviterName} invited you to join {model.ProjectName} as {model.Role}.";
        if (!string.IsNullOrWhiteSpace(model.Message))
        {
            body += $" Message: {model.Message}";
        }

        var text = $"{body} Open invitation: {model.InvitationUrl}";
        return new EmailMessage(email, subject, Layout(subject, "Project invitation", body, model.InvitationUrl, "View invitation"), text);
    }

    public static EmailMessage Notification(string email, NotificationEmailModel model)
    {
        var text = $"{model.Title}\n\n{model.Body}";
        if (!string.IsNullOrWhiteSpace(model.ActionUrl))
        {
            text += $"\n\n{model.ActionUrl}";
        }

        return new EmailMessage(email, model.Subject, Layout(model.Subject, model.Title, model.Body, model.ActionUrl, model.ActionText), text);
    }

    private static string Layout(string subject, string heading, string body, string? actionUrl, string? actionText)
    {
        var safeSubject = WebUtility.HtmlEncode(subject);
        var safeHeading = WebUtility.HtmlEncode(heading);
        var safeBody = WebUtility.HtmlEncode(body);
        var action = string.IsNullOrWhiteSpace(actionUrl)
            ? string.Empty
            : $"""
              <p style="margin:24px 0">
                <a href="{WebUtility.HtmlEncode(actionUrl)}" style="background:#0f766e;color:#ffffff;padding:12px 18px;border-radius:6px;text-decoration:none;font-weight:600">{WebUtility.HtmlEncode(actionText ?? "Open")}</a>
              </p>
              """;

        return $"""
               <!doctype html>
               <html>
               <body style="margin:0;background:#f8fafc;font-family:Arial,sans-serif;color:#0f172a">
                 <div style="max-width:640px;margin:0 auto;padding:32px 20px">
                   <div style="background:#ffffff;border:1px solid #e2e8f0;border-radius:8px;padding:28px">
                     <p style="margin:0 0 16px;color:#0f766e;font-weight:700">StartupConnect</p>
                     <h1 style="font-size:22px;line-height:1.3;margin:0 0 12px">{safeHeading}</h1>
                     <p style="font-size:15px;line-height:1.7;margin:0;color:#334155">{safeBody}</p>
                     {action}
                     <p style="font-size:12px;line-height:1.6;color:#64748b;margin:24px 0 0">If you did not request this email, you can safely ignore it.</p>
                   </div>
                 </div>
               </body>
               </html>
               """;
    }
}
