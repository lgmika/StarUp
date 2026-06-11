namespace StartupConnect.Domain.Entities;

public sealed class Skill : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public ICollection<UserSkill> UserSkills { get; set; } = [];
}

