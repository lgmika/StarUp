namespace StartupConnect.Domain.Entities;

public sealed class InterviewParticipant : BaseEntity
{
    public Guid InterviewId { get; set; }

    public ProjectInterview Interview { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public bool IsRequired { get; set; } = true;
}
