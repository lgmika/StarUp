using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class InterviewStatusHistory : BaseEntity
{
    public Guid InterviewId { get; set; }

    public ProjectInterview Interview { get; set; } = null!;

    public InterviewStatus FromStatus { get; set; }

    public InterviewStatus ToStatus { get; set; }

    public Guid ChangedByUserId { get; set; }

    public User ChangedByUser { get; set; } = null!;

    public string? Reason { get; set; }
}
