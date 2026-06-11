using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class Project : BaseEntity
{
    public Guid OwnerUserId { get; set; }

    public User OwnerUser { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Problem { get; set; } = string.Empty;

    public string Solution { get; set; } = string.Empty;

    public string? TargetMarket { get; set; }

    public string? BusinessModel { get; set; }

    public string? FundingNeeds { get; set; }

    public string? PitchDeckUrl { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;

    public ProjectStage Stage { get; set; } = ProjectStage.Idea;

    public bool IsRecruiting { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    public ICollection<ProjectMember> Members { get; set; } = [];
}

