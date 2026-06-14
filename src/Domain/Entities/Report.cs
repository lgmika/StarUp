using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class Report : BaseEntity
{
    public Guid ReporterUserId { get; set; }

    public User ReporterUser { get; set; } = null!;

    public string TargetType { get; set; } = string.Empty;

    public Guid TargetId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public ReportReasonCode ReasonCode { get; set; } = ReportReasonCode.Other;

    public string Description { get; set; } = string.Empty;

    public string? Evidence { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public Guid? AssignedModeratorId { get; set; }

    public User? AssignedModerator { get; set; }

    public string? Resolution { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }
}
