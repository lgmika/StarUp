namespace StartupConnect.Domain.Entities;

public sealed class UserSkill
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid SkillId { get; set; }

    public Skill Skill { get; set; } = null!;

    public int? YearsOfExperience { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

