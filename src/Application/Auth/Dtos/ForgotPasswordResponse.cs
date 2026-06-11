namespace StartupConnect.Application.Auth.Dtos;

public sealed record ForgotPasswordResponse(string Message, string? DevPasswordResetToken = null);

