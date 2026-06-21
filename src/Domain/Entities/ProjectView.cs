namespace StartupConnect.Domain.Entities;

public sealed class ProjectView : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid? ViewerUserId { get; set; }

    public User? ViewerUser { get; set; }

    public Guid? VisitorId { get; set; }

    public DateOnly ViewedOn { get; set; }
}
