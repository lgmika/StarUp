using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class Report : BaseEntity
{
    public Guid ReporterUserId { get; set; }

    public User ReporterUser { get; set; } = null!;

    public string TargetType { get; set; } = string.Empty;

    public Guid TargetId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public ReportStatus Status { get; set; } = ReportStatus.Pending;
}

