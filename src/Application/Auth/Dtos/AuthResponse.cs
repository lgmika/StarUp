namespace StartupConnect.Application.Auth.Dtos;

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    AuthUserDto User,
    string? DevEmailVerificationToken = null);

