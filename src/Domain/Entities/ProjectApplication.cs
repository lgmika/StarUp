using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class ProjectApplication : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid ApplicantUserId { get; set; }

    public User ApplicantUser { get; set; } = null!;

    public Guid? CvId { get; set; }

    public Cv? Cv { get; set; }

    public string CoverLetter { get; set; } = string.Empty;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    public string? FounderNote { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }
}

