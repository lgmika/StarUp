namespace StartupConnect.Application.Auth.Dtos;

public sealed record AuthUserDto(
    Guid Id,
    string Email,
    string FullName,
    bool IsEmailVerified,
    IReadOnlyCollection<string> Roles);

