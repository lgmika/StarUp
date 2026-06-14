using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }

    public bool IsSuspended { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;

    public DateTimeOffset? SuspendedUntil { get; set; }

    public string? SuspensionReason { get; set; }

    public DateTimeOffset? BannedAt { get; set; }

    public string? BanReason { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
