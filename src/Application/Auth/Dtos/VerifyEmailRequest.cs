namespace StartupConnect.Application.Auth.Dtos;

public sealed record VerifyEmailRequest(string Email, string Token);

