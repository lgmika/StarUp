using System.Security.Claims;
using StartupConnect.Application.Reports.Dtos;

namespace StartupConnect.Application.Reports.Interfaces;

public interface IReportService
{
    Task<ReportDto> CreateAsync(
        ClaimsPrincipal principal,
        CreateReportRequest request,
        CancellationToken cancellationToken);

    Task<ReportListResponse> GetMyReportsAsync(
        ClaimsPrincipal principal,
        ReportQuery query,
        CancellationToken cancellationToken);

    Task<ReportDetailDto> GetMyReportAsync(
        ClaimsPrincipal principal,
        Guid reportId,
        CancellationToken cancellationToken);

    Task<ReportTargetContextDto> GetTargetContextAsync(
        ClaimsPrincipal principal,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken);

    Task<ReportListResponse> GetModeratorReportsAsync(
        ReportQuery query,
        CancellationToken cancellationToken);

    Task<ReportDetailDto> GetModeratorReportAsync(
        Guid reportId,
        CancellationToken cancellationToken);

    Task<ReportDetailDto> AssignAsync(
        ClaimsPrincipal principal,
        Guid reportId,
        ModeratorReportActionRequest request,
        CancellationToken cancellationToken);

    Task<ReportDetailDto> InvestigateAsync(
        ClaimsPrincipal principal,
        Guid reportId,
        ModeratorReportActionRequest request,
        CancellationToken cancellationToken);

    Task<ReportDetailDto> ResolveAsync(
        ClaimsPrincipal principal,
        Guid reportId,
        ResolveReportRequest request,
        CancellationToken cancellationToken);

    Task<ReportDetailDto> DismissAsync(
        ClaimsPrincipal principal,
        Guid reportId,
        ModeratorReportActionRequest request,
        CancellationToken cancellationToken);
}
