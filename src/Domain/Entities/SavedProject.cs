namespace StartupConnect.Domain.Entities;

public sealed class SavedProject
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

