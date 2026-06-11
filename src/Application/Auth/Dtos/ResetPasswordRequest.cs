namespace StartupConnect.Application.Auth.Dtos;

public sealed record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword);

