namespace StartupConnect.Application.Email.Models;

public sealed record NotificationEmailModel(
    string Subject,
    string Title,
    string Body,
    string? ActionUrl = null,
    string? ActionText = null);
