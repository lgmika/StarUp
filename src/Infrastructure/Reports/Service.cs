using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Notifications.Dtos;
using StartupConnect.Application.Notifications.Interfaces;
using StartupConnect.Application.Realtime.Interfaces;
using StartupConnect.Application.Reports.Dtos;
using StartupConnect.Application.Reports.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.Reports;

public sealed class ReportService(
    AppDbContext dbContext,
    INotificationService notificationService,
    IRealtimeNotifier realtimeNotifier) : IReportService
{
    private const int DailyReportLimit = 10;
    private const int MaxPageSize = 100;

    public async Task<ReportDto> CreateAsync(
        ClaimsPrincipal principal,
        CreateReportRequest request,
        CancellationToken cancellationToken)
    {
        var reporterUserId = GetUserId(principal);
        ValidateCreate(request, reporterUserId);
        var context = await BuildTargetContextAsync(reporterUserId, request.TargetType, request.TargetId, cancellationToken);
        if (!context.Exists)
        {
            throw new ApiException("Report target not found", HttpStatusCode.NotFound);
        }

        if (!context.CanReport)
        {
            throw new ApiException(context.Reason ?? "This target cannot be reported", HttpStatusCode.BadRequest);
        }

        await EnsureRateLimitAsync(reporterUserId, cancellationToken);

        var duplicate = await dbContext.Reports.FirstOrDefaultAsync(report =>
            report.ReporterUserId == reporterUserId &&
            report.TargetType == NormalizeTargetType(request.TargetType) &&
            report.TargetId == request.TargetId &&
            (report.Status == ReportStatus.Pending || report.Status == ReportStatus.Investigating || report.Status == ReportStatus.Escalated),
            cancellationToken);

        if (duplicate is not null)
        {
            return await MapReportAsync(duplicate, cancellationToken);
        }

        var report = new Report
        {
            ReporterUserId = reporterUserId,
            TargetType = NormalizeTargetType(request.TargetType),
            TargetId = request.TargetId,
            ReasonCode = request.ReasonCode,
            Description = request.Description.Trim(),
            Evidence = string.IsNullOrWhiteSpace(request.Evidence) ? null : request.Evidence.Trim(),
            Reason = $"{request.ReasonCode}: {request.Description.Trim()}",
            Status = ReportStatus.Pending
        };

        dbContext.Reports.Add(report);
        AddAction(report, reporterUserId, "Created", "Report submitted");
        AddAudit(reporterUserId, "Report.Create", "Report", report.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = await MapReportAsync(report, cancellationToken);
        await realtimeNotifier.ReportChangedAsync(report.Id, result, cancellationToken);
        return result;
    }

    public async Task<ReportListResponse> GetMyReportsAsync(
        ClaimsPrincipal principal,
        ReportQuery query,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        return await GetReportsAsync(dbContext.Reports.Where(report => report.ReporterUserId == userId), query, cancellationToken);
    }

    public async Task<ReportDetailDto> GetMyReportAsync(
        ClaimsPrincipal principal,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var report = await dbContext.Reports.FirstOrDefaultAsync(
            item => item.Id == reportId && item.ReporterUserId == userId,
            cancellationToken)
            ?? throw new ApiException("Report not found", HttpStatusCode.NotFound);

        return await MapDetailAsync(report, cancellationToken);
    }

    public async Task<ReportTargetContextDto> GetTargetContextAsync(
        ClaimsPrincipal principal,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        return await BuildTargetContextAsync(userId, targetType, targetId, cancellationToken);
    }

    public Task<ReportListResponse> GetModeratorReportsAsync(
        ReportQuery query,
        CancellationToken cancellationToken)
    {
        return GetReportsAsync(dbContext.Reports, query, cancellationToken);
    }

    public async Task<ReportDetailDto> GetModeratorReportAsync(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var report = await GetReportAsync(reportId, cancellationToken);
        return await MapDetailAsync(report, cancellationToken);
    }

    public async Task<ReportDetailDto> AssignAsync(
        ClaimsPrincipal principal,
        Guid reportId,
        ModeratorReportActionRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateReason(request.Reason);
        var report = await GetReportAsync(reportId, cancellationToken);

        report.AssignedModeratorId = actorUserId;
        report.UpdatedAt = DateTimeOffset.UtcNow;
        AddAction(report, actorUserId, "Assigned", request.Reason);
        AddAudit(actorUserId, "Report.Assign", "Report", report.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await NotifyAndReturnDetailAsync(report, cancellationToken);
    }

    public async Task<ReportDetailDto> InvestigateAsync(
        ClaimsPrincipal principal,
        Guid reportId,
        ModeratorReportActionRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateReason(request.Reason);
        var report = await GetReportAsync(reportId, cancellationToken);

        report.Status = ReportStatus.Investigating;
        report.AssignedModeratorId ??= actorUserId;
        report.UpdatedAt = DateTimeOffset.UtcNow;
        AddAction(report, actorUserId, "Investigating", request.Reason);
        AddAudit(actorUserId, "Report.Investigate", "Report", report.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await NotifyAndReturnDetailAsync(report, cancellationToken);
    }

    public async Task<ReportDetailDto> ResolveAsync(
        ClaimsPrincipal principal,
        Guid reportId,
        ResolveReportRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateReason(request.Resolution);
        var report = await GetReportAsync(reportId, cancellationToken);

        report.Status = ReportStatus.Resolved;
        report.Resolution = request.Resolution.Trim();
        report.ResolvedAt = DateTimeOffset.UtcNow;
        report.AssignedModeratorId ??= actorUserId;
        report.UpdatedAt = DateTimeOffset.UtcNow;
        AddAction(report, actorUserId, "Resolved", request.Resolution);
        AddAudit(actorUserId, "Report.Resolve", "Report", report.Id);
        await NotifyReporterAsync(report, "Report resolved", "Your report has been reviewed and resolved.", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await NotifyAndReturnDetailAsync(report, cancellationToken);
    }

    public async Task<ReportDetailDto> DismissAsync(
        ClaimsPrincipal principal,
        Guid reportId,
        ModeratorReportActionRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateReason(request.Reason);
        var report = await GetReportAsync(reportId, cancellationToken);

        report.Status = ReportStatus.Dismissed;
        report.Resolution = request.Reason.Trim();
        report.ResolvedAt = DateTimeOffset.UtcNow;
        report.AssignedModeratorId ??= actorUserId;
        report.UpdatedAt = DateTimeOffset.UtcNow;
        AddAction(report, actorUserId, "Dismissed", request.Reason);
        AddAudit(actorUserId, "Report.Dismiss", "Report", report.Id);
        await NotifyReporterAsync(report, "Report dismissed", "Your report has been reviewed and dismissed.", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await NotifyAndReturnDetailAsync(report, cancellationToken);
    }

    private async Task<ReportDetailDto> NotifyAndReturnDetailAsync(Report report, CancellationToken cancellationToken)
    {
        var detail = await MapDetailAsync(report, cancellationToken);
        await realtimeNotifier.ReportChangedAsync(report.Id, detail, cancellationToken);
        return detail;
    }

    private async Task<ReportListResponse> GetReportsAsync(
        IQueryable<Report> baseQuery,
        ReportQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var reports = ApplyFilters(baseQuery, query);
        var total = await reports.CountAsync(cancellationToken);
        var items = await reports
            .OrderByDescending(report => report.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        var mapped = new List<ReportDto>();
        foreach (var report in items)
        {
            mapped.Add(await MapReportAsync(report, cancellationToken));
        }

        return new ReportListResponse(mapped, total, page, pageSize);
    }

    private static IQueryable<Report> ApplyFilters(IQueryable<Report> query, ReportQuery filter)
    {
        if (filter.Status.HasValue)
        {
            query = query.Where(report => report.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetType))
        {
            var targetType = NormalizeTargetType(filter.TargetType);
            query = query.Where(report => report.TargetType == targetType);
        }

        if (filter.ReasonCode.HasValue)
        {
            query = query.Where(report => report.ReasonCode == filter.ReasonCode.Value);
        }

        return query;
    }

    private async Task<Report> GetReportAsync(Guid reportId, CancellationToken cancellationToken)
    {
        return await dbContext.Reports.FirstOrDefaultAsync(report => report.Id == reportId, cancellationToken)
            ?? throw new ApiException("Report not found", HttpStatusCode.NotFound);
    }

    private async Task<ReportTargetContextDto> BuildTargetContextAsync(
        Guid reporterUserId,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        var normalizedType = NormalizeTargetType(targetType);
        if (!IsSupportedTargetType(normalizedType))
        {
            return new ReportTargetContextDto(normalizedType, targetId, false, false, null, null, "Target type is not supported");
        }

        if (targetId == Guid.Empty)
        {
            return new ReportTargetContextDto(normalizedType, targetId, false, false, null, null, "Target id is required");
        }

        return normalizedType switch
        {
            "User" => await BuildUserTargetContextAsync(reporterUserId, normalizedType, targetId, cancellationToken),
            "Project" => await BuildProjectTargetContextAsync(reporterUserId, normalizedType, targetId, cancellationToken),
            "Portfolio" => await BuildPortfolioTargetContextAsync(reporterUserId, normalizedType, targetId, cancellationToken),
            "Application" => await BuildApplicationTargetContextAsync(reporterUserId, normalizedType, targetId, cancellationToken),
            "Message" => await BuildMessageTargetContextAsync(reporterUserId, normalizedType, targetId, cancellationToken),
            _ => new ReportTargetContextDto(normalizedType, targetId, false, false, null, null, "Target type is not supported")
        };
    }

    private async Task<ReportTargetContextDto> BuildUserTargetContextAsync(
        Guid reporterUserId,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(item => item.Id == targetId && !item.IsDeleted, cancellationToken);
        if (user is null)
        {
            return new ReportTargetContextDto(targetType, targetId, false, false, null, null, "User not found");
        }

        return new ReportTargetContextDto(
            targetType,
            targetId,
            true,
            reporterUserId != user.Id,
            user.FullName,
            user.Email,
            reporterUserId == user.Id ? "You cannot report yourself" : null);
    }

    private async Task<ReportTargetContextDto> BuildProjectTargetContextAsync(
        Guid reporterUserId,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .Include(item => item.OwnerUser)
            .FirstOrDefaultAsync(item => item.Id == targetId && !item.IsDeleted, cancellationToken);
        if (project is null)
        {
            return new ReportTargetContextDto(targetType, targetId, false, false, null, null, "Project not found");
        }

        return new ReportTargetContextDto(
            targetType,
            targetId,
            true,
            reporterUserId != project.OwnerUserId,
            project.Title,
            project.OwnerUser.Email,
            reporterUserId == project.OwnerUserId ? "You cannot report your own project" : null);
    }

    private async Task<ReportTargetContextDto> BuildPortfolioTargetContextAsync(
        Guid reporterUserId,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        var portfolio = await dbContext.Portfolios
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == targetId && !item.IsDeleted, cancellationToken);
        if (portfolio is null)
        {
            return new ReportTargetContextDto(targetType, targetId, false, false, null, null, "Portfolio not found");
        }

        return new ReportTargetContextDto(
            targetType,
            targetId,
            true,
            reporterUserId != portfolio.UserId,
            portfolio.Title,
            portfolio.User.Email,
            reporterUserId == portfolio.UserId ? "You cannot report your own portfolio" : null);
    }

    private async Task<ReportTargetContextDto> BuildApplicationTargetContextAsync(
        Guid reporterUserId,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        var application = await dbContext.ProjectApplications
            .Include(item => item.ApplicantUser)
            .Include(item => item.Project)
            .ThenInclude(project => project.OwnerUser)
            .FirstOrDefaultAsync(item => item.Id == targetId, cancellationToken);
        if (application is null)
        {
            return new ReportTargetContextDto(targetType, targetId, false, false, null, null, "Application not found");
        }

        var isSelf = reporterUserId == application.ApplicantUserId;
        return new ReportTargetContextDto(
            targetType,
            targetId,
            true,
            !isSelf,
            $"Application for {application.Project.Title}",
            application.ApplicantUser.Email,
            isSelf ? "You cannot report your own application" : null);
    }

    private async Task<ReportTargetContextDto> BuildMessageTargetContextAsync(
        Guid reporterUserId,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        var message = await dbContext.Messages
            .Include(item => item.SenderUser)
            .FirstOrDefaultAsync(item => item.Id == targetId && !item.IsDeleted, cancellationToken);
        if (message is null)
        {
            return new ReportTargetContextDto(targetType, targetId, false, false, null, null, "Message not found");
        }

        return new ReportTargetContextDto(
            targetType,
            targetId,
            true,
            reporterUserId != message.SenderUserId,
            message.Content.Length > 80 ? $"{message.Content[..80]}..." : message.Content,
            message.SenderUser.Email,
            reporterUserId == message.SenderUserId ? "You cannot report your own message" : null);
    }

    private async Task EnsureRateLimitAsync(Guid reporterUserId, CancellationToken cancellationToken)
    {
        var today = DateTimeOffset.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var count = await dbContext.Reports.CountAsync(report =>
            report.ReporterUserId == reporterUserId &&
            report.CreatedAt >= today &&
            report.CreatedAt < tomorrow,
            cancellationToken);

        if (count >= DailyReportLimit)
        {
            throw new ApiException("Daily report limit exceeded", HttpStatusCode.TooManyRequests);
        }
    }

    private async Task NotifyReporterAsync(Report report, string title, string message, CancellationToken cancellationToken)
    {
        await notificationService.CreateAsync(new CreateNotificationRequest(
            report.ReporterUserId,
            NotificationType.System,
            title,
            message,
            "Report",
            report.Id,
            $"/reports/{report.Id}"), cancellationToken);
    }

    private async Task<ReportDetailDto> MapDetailAsync(Report report, CancellationToken cancellationToken)
    {
        var actions = await dbContext.ReportActions
            .Where(action => action.ReportId == report.Id)
            .OrderBy(action => action.CreatedAt)
            .Select(action => new ReportActionDto(
                action.Id,
                action.ReportId,
                action.ActorUserId,
                action.Action,
                action.Reason,
                action.CreatedAt))
            .ToArrayAsync(cancellationToken);

        return new ReportDetailDto(await MapReportAsync(report, cancellationToken), actions);
    }

    private async Task<ReportDto> MapReportAsync(Report report, CancellationToken cancellationToken)
    {
        var reporterEmail = await dbContext.Users
            .Where(user => user.Id == report.ReporterUserId)
            .Select(user => user.Email)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new ReportDto(
            report.Id,
            report.ReporterUserId,
            reporterEmail,
            report.TargetType,
            report.TargetId,
            report.ReasonCode,
            string.IsNullOrWhiteSpace(report.Description) ? report.Reason : report.Description,
            report.Evidence,
            report.Status,
            report.AssignedModeratorId,
            report.Resolution,
            report.CreatedAt,
            report.ResolvedAt);
    }

    private static void ValidateCreate(CreateReportRequest request, Guid reporterUserId)
    {
        if (string.IsNullOrWhiteSpace(request.TargetType))
        {
            throw new ValidationException([new ErrorDetail("Required", "Target type is required", "targetType")]);
        }

        if (request.TargetId == Guid.Empty)
        {
            throw new ValidationException([new ErrorDetail("Required", "Target id is required", "targetId")]);
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ValidationException([new ErrorDetail("Required", "Description is required", "description")]);
        }

        if (NormalizeTargetType(request.TargetType) == "User" && request.TargetId == reporterUserId)
        {
            throw new ApiException("You cannot report yourself", HttpStatusCode.BadRequest);
        }

        if (!IsSupportedTargetType(NormalizeTargetType(request.TargetType)))
        {
            throw new ValidationException([new ErrorDetail("InvalidTargetType", "Target type is not supported", "targetType")]);
        }
    }

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException([new ErrorDetail("Required", "Reason is required", "reason")]);
        }
    }

    private static string NormalizeTargetType(string targetType)
    {
        var value = targetType.Trim();
        return string.Concat(value[..1].ToUpperInvariant(), value[1..].ToLowerInvariant());
    }

    private static bool IsSupportedTargetType(string targetType)
    {
        return targetType is "User" or "Project" or "Message" or "Portfolio" or "Application";
    }

    private void AddAction(Report report, Guid actorUserId, string action, string reason)
    {
        dbContext.ReportActions.Add(new ReportAction
        {
            Report = report,
            ActorUserId = actorUserId,
            Action = action,
            Reason = reason.Trim()
        });
    }

    private void AddAudit(Guid actorUserId, string action, string resourceType, Guid resourceId)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId
        });
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var userIdValue =
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            principal.FindFirst("sub")?.Value ??
            principal.FindFirst("nameid")?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new ApiException("Invalid access token", HttpStatusCode.Unauthorized);
        }

        return userId;
    }
}
