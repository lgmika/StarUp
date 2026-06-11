namespace StartupConnect.Domain.Entities;

public sealed class ProjectRequiredSkill
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid SkillId { get; set; }

    public Skill Skill { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

