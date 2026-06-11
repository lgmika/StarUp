namespace StartupConnect.Domain.Entities;

public sealed class ProjectVersion : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public int VersionNumber { get; set; }

    public Guid ChangedByUserId { get; set; }

    public User ChangedByUser { get; set; } = null!;

    public string SnapshotJson { get; set; } = string.Empty;

    public string ChangeReason { get; set; } = string.Empty;
}

