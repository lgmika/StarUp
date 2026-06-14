using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class ProjectInvitation : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid InvitedByUserId { get; set; }

    public User InvitedByUser { get; set; } = null!;

    public Guid? InvitedUserId { get; set; }

    public User? InvitedUser { get; set; }

    public string Email { get; set; } = string.Empty;

    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.Member;

    public string? Message { get; set; }

    public ProjectInvitationStatus Status { get; set; } = ProjectInvitationStatus.Pending;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RespondedAt { get; set; }
}
