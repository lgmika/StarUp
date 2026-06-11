using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class ProjectVisibilitySetting : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public ProjectVisibility Visibility { get; set; } = ProjectVisibility.Public;

    public bool RequiresNda { get; set; }

    public bool ShowFounderContact { get; set; }
}

