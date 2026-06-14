using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class ProjectMemberHistory : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Guid? MemberId { get; set; }

    public Guid UserId { get; set; }

    public Guid ActorUserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public ProjectMemberRole? FromRole { get; set; }

    public ProjectMemberRole? ToRole { get; set; }

    public string? Reason { get; set; }
}
