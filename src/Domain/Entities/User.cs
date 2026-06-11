namespace StartupConnect.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }

    public bool IsSuspended { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

