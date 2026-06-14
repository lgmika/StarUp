namespace StartupConnect.Infrastructure.Email;

internal sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string TextBody);
