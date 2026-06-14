using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class Notification : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public Guid? ResourceId { get; set; }

    public string? ResourceType { get; set; }

    public string? ActionUrl { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public bool IsDeleted { get; set; }
}
