using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Reports.Dtos;

public sealed record CreateReportRequest(
    string TargetType,
    Guid TargetId,
    ReportReasonCode ReasonCode,
    string Description,
    string? Evidence = null);

public sealed record ReportTargetContextDto(
    string TargetType,
    Guid TargetId,
    bool Exists,
    bool CanReport,
    string? DisplayName,
    string? OwnerEmail,
    string? Reason);

public sealed record ReportDto(
    Guid Id,
    Guid ReporterUserId,
    string ReporterEmail,
    string TargetType,
    Guid TargetId,
    ReportReasonCode ReasonCode,
    string Description,
    string? Evidence,
    ReportStatus Status,
    Guid? AssignedModeratorId,
    string? Resolution,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);

public sealed record ReportActionDto(
    Guid Id,
    Guid ReportId,
    Guid ActorUserId,
    string Action,
    string Reason,
    DateTimeOffset CreatedAt);

public sealed record ReportDetailDto(
    ReportDto Report,
    IReadOnlyCollection<ReportActionDto> Actions);

public sealed record ReportListResponse(
    IReadOnlyCollection<ReportDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record ReportQuery(
    ReportStatus? Status = null,
    string? TargetType = null,
    ReportReasonCode? ReasonCode = null,
    int Page = 1,
    int PageSize = 20);

public sealed record ModeratorReportActionRequest(string Reason);

public sealed record ResolveReportRequest(string Resolution);
