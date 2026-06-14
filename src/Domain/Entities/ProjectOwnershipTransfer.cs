using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class ProjectOwnershipTransfer : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid FromUserId { get; set; }

    public User FromUser { get; set; } = null!;

    public Guid ToUserId { get; set; }

    public User ToUser { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public ProjectOwnershipTransferStatus Status { get; set; } = ProjectOwnershipTransferStatus.Pending;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }
}
