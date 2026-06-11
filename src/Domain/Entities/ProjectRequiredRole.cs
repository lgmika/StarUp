namespace StartupConnect.Domain.Entities;

public sealed class ProjectRequiredRole : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public string RoleName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Slots { get; set; } = 1;

    public bool IsOpen { get; set; } = true;
}

