namespace StartupConnect.Domain.Entities;

public sealed class ReportAction : BaseEntity
{
    public Guid ReportId { get; set; }

    public Report Report { get; set; } = null!;

    public Guid ActorUserId { get; set; }

    public User ActorUser { get; set; } = null!;

    public string Action { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}

