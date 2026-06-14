namespace StartupConnect.Application.Email.Models;

public sealed record ProjectInvitationEmailModel(
    string ProjectName,
    string InviterName,
    string Role,
    string InvitationUrl,
    string? Message = null);
