namespace StartupConnect.Domain.Entities;

public sealed class Portfolio : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsDeleted { get; set; }
}

