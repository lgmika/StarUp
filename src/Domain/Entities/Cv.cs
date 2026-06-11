using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class Cv : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public string? ExperienceJson { get; set; }

    public string? EducationJson { get; set; }

    public CvType Type { get; set; } = CvType.Internal;

    public Guid? FileId { get; set; }

    public StoredFile? File { get; set; }

    public bool IsDefault { get; set; }

    public bool IsDeleted { get; set; }
}

