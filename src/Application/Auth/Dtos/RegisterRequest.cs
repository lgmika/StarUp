namespace StartupConnect.Application.Auth.Dtos;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FullName);

