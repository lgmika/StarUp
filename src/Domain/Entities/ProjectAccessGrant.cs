namespace StartupConnect.Domain.Entities;

public sealed class ProjectAccessGrant : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string AccessLevel { get; set; } = "View";

    public DateTimeOffset? ExpiresAt { get; set; }
}

