using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class ProjectMember : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.Member;

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive { get; set; } = true;
}

