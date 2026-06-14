using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class ProjectInterview : BaseEntity
{
    public Guid ApplicationId { get; set; }

    public ProjectApplication Application { get; set; } = null!;

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid ScheduledByUserId { get; set; }

    public User ScheduledByUser { get; set; } = null!;

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public string TimeZone { get; set; } = "UTC";

    public InterviewMeetingType MeetingType { get; set; } = InterviewMeetingType.Online;

    public string? MeetingUrl { get; set; }

    public string? Location { get; set; }

    public string? Note { get; set; }

    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;

    public string? CancellationReason { get; set; }
}
