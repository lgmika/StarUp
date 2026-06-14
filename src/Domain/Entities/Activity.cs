using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class Activity : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid? ActorUserId { get; set; }

    public User? ActorUser { get; set; }

    public ActivityType Type { get; set; }

    public ActivityVisibility Visibility { get; set; } = ActivityVisibility.MembersOnly;

    public string Title { get; set; } = string.Empty;

    public string? Message { get; set; }

    public string? TargetType { get; set; }

    public Guid? TargetId { get; set; }

    public string? MetadataJson { get; set; }
}
