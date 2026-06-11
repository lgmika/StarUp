using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class ApplicationStatusHistory : BaseEntity
{
    public Guid ApplicationId { get; set; }

    public ProjectApplication Application { get; set; } = null!;

    public ApplicationStatus FromStatus { get; set; }

    public ApplicationStatus ToStatus { get; set; }

    public Guid ChangedByUserId { get; set; }

    public User ChangedByUser { get; set; } = null!;

    public string? Reason { get; set; }
}

