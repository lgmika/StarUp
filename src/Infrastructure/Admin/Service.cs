using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using StartupConnect.Application.Admin.Dtos;
using StartupConnect.Application.Admin.Interfaces;
using StartupConnect.Application.Realtime.Interfaces;
using StartupConnect.Domain.Constants;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Infrastructure.Email;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.Admin;

public sealed class AdminService(
    AppDbContext dbContext,
    IConfiguration configuration,
    IRealtimeNotifier realtimeNotifier,
    IOptions<EmailOutboxOptions> emailOutboxOptionsAccessor) : IAdminService
{
    private const int MaxPageSize = 100;
    private readonly EmailOutboxOptions emailOutboxOptions = emailOutboxOptionsAccessor.Value;

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-7);

        return new AdminDashboardDto(
            await dbContext.Users.CountAsync(user => !user.IsDeleted, cancellationToken),
            await dbContext.Users.CountAsync(user => !user.IsDeleted && user.CreatedAt >= since, cancellationToken),
            await dbContext.Users.CountAsync(user => !user.IsDeleted && user.Status == UserStatus.Active, cancellationToken),
            await dbContext.Users.CountAsync(user => !user.IsDeleted && user.IsEmailVerified, cancellationToken),
            await dbContext.Projects.CountAsync(project => !project.IsDeleted, cancellationToken),
            await dbContext.Projects.CountAsync(project => project.Status == ProjectStatus.PendingReview && !project.IsDeleted, cancellationToken),
            await dbContext.Reports.CountAsync(report => report.Status == ReportStatus.Pending || report.Status == ReportStatus.Investigating || report.Status == ReportStatus.Escalated, cancellationToken),
            await dbContext.ProjectApplications.CountAsync(cancellationToken),
            await dbContext.InvestorProfiles.CountAsync(cancellationToken),
            await dbContext.AIRequests.CountAsync(cancellationToken),
            await dbContext.Files.SumAsync(file => (long?)file.SizeInBytes, cancellationToken) ?? 0);
    }

    public async Task<AdminUserListResponse> GetUsersAsync(AdminUserQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var users = dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            users = users.Where(user =>
                user.NormalizedEmail.Contains(search) ||
                user.FullName.ToUpper().Contains(search));
        }

        if (query.Status.HasValue)
        {
            users = users.Where(user => user.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.RoleCode))
        {
            var roleCode = query.RoleCode.Trim();
            users = users.Where(user => user.UserRoles.Any(userRole => userRole.Role.Code == roleCode));
        }

        var total = await users.CountAsync(cancellationToken);
        var items = await users
            .OrderByDescending(user => user.CreatedAt)
            .Skip(Pagination.GetOffset(page, pageSize))
            .Take(pageSize)
            .Select(user => MapUser(user))
            .ToArrayAsync(cancellationToken);

        return new AdminUserListResponse(items, total, page, pageSize);
    }

    public async Task<AdminUserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserWithRolesAsync(userId, cancellationToken);
        return MapUser(user);
    }

    public async Task<AdminUserDto> SuspendUserAsync(ClaimsPrincipal principal, Guid userId, AdminUserStatusRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        EnsureNotSelf(actorUserId, userId, "You cannot suspend yourself");
        ValidateReason(request.Reason);

        var user = await GetUserWithRolesAsync(userId, cancellationToken);
        user.Status = UserStatus.Suspended;
        user.IsSuspended = true;
        user.SuspendedUntil = request.SuspendedUntil;
        user.SuspensionReason = request.Reason.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await RevokeRefreshTokensAsync(userId, cancellationToken);
        AddAudit(actorUserId, "Admin.User.Suspend", "User", userId, request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapUser(user);
    }

    public async Task<AdminUserDto> UnsuspendUserAsync(ClaimsPrincipal principal, Guid userId, AdminUserStatusRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateReason(request.Reason);

        var user = await GetUserWithRolesAsync(userId, cancellationToken);
        if (user.Status == UserStatus.Banned || user.Status == UserStatus.Deleted)
        {
            throw new ApiException("Banned or deleted users cannot be unsuspended", HttpStatusCode.BadRequest);
        }

        user.Status = UserStatus.Active;
        user.IsSuspended = false;
        user.SuspendedUntil = null;
        user.SuspensionReason = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actorUserId, "Admin.User.Unsuspend", "User", userId, request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapUser(user);
    }

    public async Task<AdminUserDto> BanUserAsync(ClaimsPrincipal principal, Guid userId, AdminUserStatusRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        EnsureNotSelf(actorUserId, userId, "You cannot ban yourself");
        ValidateReason(request.Reason);

        var user = await GetUserWithRolesAsync(userId, cancellationToken);
        user.Status = UserStatus.Banned;
        user.IsSuspended = true;
        user.BannedAt = DateTimeOffset.UtcNow;
        user.BanReason = request.Reason.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await RevokeRefreshTokensAsync(userId, cancellationToken);
        AddAudit(actorUserId, "Admin.User.Ban", "User", userId, request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapUser(user);
    }

    public async Task<AdminUserDto> UnbanUserAsync(ClaimsPrincipal principal, Guid userId, AdminUserStatusRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateReason(request.Reason);

        var user = await GetUserWithRolesAsync(userId, cancellationToken);
        if (user.Status == UserStatus.Deleted)
        {
            throw new ApiException("Deleted users cannot be unbanned", HttpStatusCode.BadRequest);
        }

        user.Status = UserStatus.Active;
        user.IsSuspended = false;
        user.BannedAt = null;
        user.BanReason = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actorUserId, "Admin.User.Unban", "User", userId, request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapUser(user);
    }

    public async Task<AdminUserDto> AddRoleAsync(ClaimsPrincipal principal, Guid userId, AdminRoleRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateRoleCode(request.RoleCode);
        var user = await GetUserWithRolesAsync(userId, cancellationToken);
        var role = await dbContext.Roles.FirstOrDefaultAsync(item => item.Code == request.RoleCode.Trim(), cancellationToken)
            ?? throw new ApiException("Role not found", HttpStatusCode.NotFound);

        if (!user.UserRoles.Any(userRole => userRole.RoleId == role.Id))
        {
            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            AddAudit(actorUserId, "Admin.UserRole.Add", "User", userId, request.RoleCode);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapUser(await GetUserWithRolesAsync(userId, cancellationToken));
    }

    public async Task<AdminUserDto> RemoveRoleAsync(ClaimsPrincipal principal, Guid userId, string roleCode, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateRoleCode(roleCode);
        var user = await GetUserWithRolesAsync(userId, cancellationToken);
        var userRole = user.UserRoles.FirstOrDefault(item => item.Role.Code == roleCode.Trim());
        if (userRole is null)
        {
            return MapUser(user);
        }

        if (roleCode.Trim() == SystemRoles.Admin)
        {
            var adminCount = await dbContext.UserRoles.CountAsync(item => item.Role.Code == SystemRoles.Admin, cancellationToken);
            if (adminCount <= 1)
            {
                throw new ApiException("Cannot remove the last Admin role", HttpStatusCode.BadRequest);
            }
        }

        dbContext.UserRoles.Remove(userRole);
        AddAudit(actorUserId, "Admin.UserRole.Remove", "User", userId, roleCode);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapUser(await GetUserWithRolesAsync(userId, cancellationToken));
    }

    public async Task<IReadOnlyCollection<AdminRoleDto>> GetRolesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Roles
            .OrderBy(role => role.Code)
            .Select(role => new AdminRoleDto(role.Id, role.Code, role.Name, role.Description))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<AdminAuditLogListResponse> GetAuditLogsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var total = await dbContext.AuditLogs.CountAsync(cancellationToken);
        var items = await dbContext.AuditLogs
            .OrderByDescending(log => log.CreatedAt)
            .Skip(Pagination.GetOffset(page, pageSize))
            .Take(pageSize)
            .Select(log => new AdminAuditLogDto(
                log.Id,
                log.ActorUserId,
                log.Action,
                log.ResourceType,
                log.ResourceId,
                log.Reason,
                log.MetadataJson,
                log.IpAddress,
                log.UserAgent,
                log.CreatedAt))
            .ToArrayAsync(cancellationToken);

        return new AdminAuditLogListResponse(items, total, page, pageSize);
    }

    public async Task<IReadOnlyCollection<AdminSettingDto>> GetSettingsAsync(CancellationToken cancellationToken)
    {
        await EnsureDefaultSettingsAsync(cancellationToken);

        return await dbContext.SystemSettings
            .OrderBy(setting => setting.Group)
            .ThenBy(setting => setting.Key)
            .Select(setting => MapSetting(setting))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<AdminSettingDto> UpdateSettingAsync(
        ClaimsPrincipal principal,
        string key,
        UpdateAdminSettingRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ValidationException([new ErrorDetail("Required", "Setting key is required", "key")]);
        }

        var setting = await dbContext.SystemSettings.FirstOrDefaultAsync(
            item => item.Key == key.Trim(),
            cancellationToken)
            ?? throw new ApiException("Setting not found", HttpStatusCode.NotFound);

        if (setting.IsReadonly)
        {
            throw new ApiException("This setting is readonly", HttpStatusCode.BadRequest);
        }

        ValidateSettingValue(setting, request.Value);
        setting.Value = request.Value.Trim();
        setting.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actorUserId, "Admin.Setting.Update", "SystemSetting", setting.Id, request.Reason ?? setting.Key);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapSetting(setting);
    }

    public async Task<AdminProjectListResponse> GetProjectsAsync(AdminProjectQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var projects = dbContext.Projects
            .Include(project => project.OwnerUser)
            .GroupJoin(
                dbContext.ProjectVisibilitySettings,
                project => project.Id,
                setting => setting.ProjectId,
                (project, settings) => new { Project = project, Setting = settings.FirstOrDefault() })
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            projects = projects.Where(item =>
                item.Project.Title.ToUpper().Contains(search) ||
                item.Project.Summary.ToUpper().Contains(search) ||
                item.Project.Slug.ToUpper().Contains(search));
        }

        if (query.Status.HasValue)
        {
            projects = projects.Where(item => item.Project.Status == query.Status.Value);
        }

        if (query.Stage.HasValue)
        {
            projects = projects.Where(item => item.Project.Stage == query.Stage.Value);
        }

        if (query.Visibility.HasValue)
        {
            projects = projects.Where(item => item.Setting != null && item.Setting.Visibility == query.Visibility.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.OwnerEmail))
        {
            var ownerEmail = query.OwnerEmail.Trim().ToUpperInvariant();
            projects = projects.Where(item => item.Project.OwnerUser.NormalizedEmail.Contains(ownerEmail));
        }

        var total = await projects.CountAsync(cancellationToken);
        var items = await projects
            .OrderByDescending(item => item.Project.CreatedAt)
            .Skip(Pagination.GetOffset(page, pageSize))
            .Take(pageSize)
            .Select(item => MapProject(item.Project, item.Setting))
            .ToArrayAsync(cancellationToken);

        return new AdminProjectListResponse(items, total, page, pageSize);
    }

    public Task<AdminProjectDto> HideProjectAsync(
        ClaimsPrincipal principal,
        Guid projectId,
        AdminProjectActionRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateProjectStatusAsync(principal, projectId, ProjectStatus.Hidden, "Admin.Project.Hide", request.Reason, cancellationToken);
    }

    public Task<AdminProjectDto> RestoreProjectAsync(
        ClaimsPrincipal principal,
        Guid projectId,
        AdminProjectActionRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateProjectStatusAsync(principal, projectId, ProjectStatus.Published, "Admin.Project.Restore", request.Reason, cancellationToken);
    }

    public Task<AdminProjectDto> ArchiveProjectAsync(
        ClaimsPrincipal principal,
        Guid projectId,
        AdminProjectActionRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateProjectStatusAsync(principal, projectId, ProjectStatus.Archived, "Admin.Project.Archive", request.Reason, cancellationToken);
    }

    public Task<AdminProjectDto> CloseProjectAsync(
        ClaimsPrincipal principal,
        Guid projectId,
        AdminProjectActionRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateProjectStatusAsync(principal, projectId, ProjectStatus.Closed, "Admin.Project.Close", request.Reason, cancellationToken);
    }

    public Task<AdminProjectDto> ForceProjectStatusAsync(
        ClaimsPrincipal principal,
        Guid projectId,
        AdminProjectStatusRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateProjectStatusAsync(principal, projectId, request.Status, "Admin.Project.ForceStatus", request.Reason, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AdminSubscriptionPlanDto>> GetSubscriptionPlansAsync(CancellationToken cancellationToken)
    {
        var plans = await dbContext.SubscriptionPlans
            .OrderBy(plan => plan.MonthlyPrice)
            .ThenBy(plan => plan.Code)
            .ToArrayAsync(cancellationToken);
        var planIds = plans.Select(plan => plan.Id).ToArray();
        var quotas = await dbContext.UsageQuotas
            .Where(quota => planIds.Contains(quota.PlanId))
            .OrderBy(quota => quota.ResourceKey)
            .ToArrayAsync(cancellationToken);

        return plans
            .Select(plan => MapPlan(plan, quotas.Where(quota => quota.PlanId == plan.Id).ToArray()))
            .ToArray();
    }

    public async Task<AdminSubscriptionPlanDto> CreateSubscriptionPlanAsync(
        ClaimsPrincipal principal,
        AdminSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidatePlanRequest(request);
        var code = request.Code.Trim();
        if (await dbContext.SubscriptionPlans.AnyAsync(plan => plan.Code == code, cancellationToken))
        {
            throw new ApiException("Subscription plan code already exists", HttpStatusCode.Conflict);
        }

        var plan = new SubscriptionPlan
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            MonthlyPrice = request.MonthlyPrice,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            IsActive = request.IsActive
        };
        dbContext.SubscriptionPlans.Add(plan);
        AddAudit(actorUserId, "Admin.SubscriptionPlan.Create", "SubscriptionPlan", plan.Id, request.Reason ?? code);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapPlan(plan, []);
    }

    public async Task<AdminSubscriptionPlanDto> UpdateSubscriptionPlanAsync(
        ClaimsPrincipal principal,
        Guid planId,
        AdminSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidatePlanRequest(request);
        var plan = await dbContext.SubscriptionPlans.FirstOrDefaultAsync(item => item.Id == planId, cancellationToken)
            ?? throw new ApiException("Subscription plan not found", HttpStatusCode.NotFound);
        var code = request.Code.Trim();
        var duplicate = await dbContext.SubscriptionPlans.AnyAsync(item => item.Id != planId && item.Code == code, cancellationToken);
        if (duplicate)
        {
            throw new ApiException("Subscription plan code already exists", HttpStatusCode.Conflict);
        }

        plan.Code = code;
        plan.Name = request.Name.Trim();
        plan.Description = request.Description.Trim();
        plan.MonthlyPrice = request.MonthlyPrice;
        plan.Currency = request.Currency.Trim().ToUpperInvariant();
        plan.IsActive = request.IsActive;
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actorUserId, "Admin.SubscriptionPlan.Update", "SubscriptionPlan", plan.Id, request.Reason ?? code);
        await dbContext.SaveChangesAsync(cancellationToken);

        var quotas = await dbContext.UsageQuotas.Where(quota => quota.PlanId == plan.Id).OrderBy(quota => quota.ResourceKey).ToArrayAsync(cancellationToken);
        return MapPlan(plan, quotas);
    }

    public async Task<AdminUsageQuotaDto> CreateUsageQuotaAsync(
        ClaimsPrincipal principal,
        Guid planId,
        AdminUsageQuotaRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateQuotaRequest(request);
        await EnsurePlanExistsAsync(planId, cancellationToken);
        var resourceKey = request.ResourceKey.Trim();
        if (await dbContext.UsageQuotas.AnyAsync(quota => quota.PlanId == planId && quota.ResourceKey == resourceKey, cancellationToken))
        {
            throw new ApiException("Usage quota already exists for this plan", HttpStatusCode.Conflict);
        }

        var quota = new UsageQuota { PlanId = planId, ResourceKey = resourceKey, Limit = request.Limit };
        dbContext.UsageQuotas.Add(quota);
        AddAudit(actorUserId, "Admin.UsageQuota.Create", "UsageQuota", quota.Id, request.Reason ?? resourceKey);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapQuota(quota);
    }

    public async Task<AdminUsageQuotaDto> UpdateUsageQuotaAsync(
        ClaimsPrincipal principal,
        Guid planId,
        Guid quotaId,
        AdminUsageQuotaRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateQuotaRequest(request);
        var quota = await dbContext.UsageQuotas.FirstOrDefaultAsync(item => item.Id == quotaId && item.PlanId == planId, cancellationToken)
            ?? throw new ApiException("Usage quota not found", HttpStatusCode.NotFound);
        var resourceKey = request.ResourceKey.Trim();
        var duplicate = await dbContext.UsageQuotas.AnyAsync(
            item => item.Id != quotaId && item.PlanId == planId && item.ResourceKey == resourceKey,
            cancellationToken);
        if (duplicate)
        {
            throw new ApiException("Usage quota already exists for this plan", HttpStatusCode.Conflict);
        }

        quota.ResourceKey = resourceKey;
        quota.Limit = request.Limit;
        quota.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actorUserId, "Admin.UsageQuota.Update", "UsageQuota", quota.Id, request.Reason ?? resourceKey);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapQuota(quota);
    }

    public async Task DeleteUsageQuotaAsync(
        ClaimsPrincipal principal,
        Guid planId,
        Guid quotaId,
        string? reason,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        var quota = await dbContext.UsageQuotas.FirstOrDefaultAsync(item => item.Id == quotaId && item.PlanId == planId, cancellationToken)
            ?? throw new ApiException("Usage quota not found", HttpStatusCode.NotFound);

        dbContext.UsageQuotas.Remove(quota);
        AddAudit(actorUserId, "Admin.UsageQuota.Delete", "UsageQuota", quota.Id, reason ?? quota.ResourceKey);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminEmailOutboxListResponse> GetEmailOutboxAsync(
        AdminEmailOutboxQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var now = DateTimeOffset.UtcNow;
        var messages = dbContext.EmailOutboxMessages.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Recipient))
        {
            var recipient = query.Recipient.Trim();
            messages = messages.Where(message => EF.Functions.ILike(message.Recipient, $"%{recipient}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            messages = query.Status.Trim().ToLowerInvariant() switch
            {
                "sent" => messages.Where(message => message.SentAt != null),
                "failed" => messages.Where(message => message.SentAt == null && message.Attempts >= emailOutboxOptions.MaxAttempts),
                "processing" => messages.Where(message => message.SentAt == null && message.LeaseId != null && message.LockedUntil > now),
                "pending" => messages.Where(message =>
                    message.SentAt == null &&
                    message.Attempts < emailOutboxOptions.MaxAttempts &&
                    (message.LeaseId == null || message.LockedUntil == null || message.LockedUntil <= now)),
                _ => throw new ValidationException([new ErrorDetail("InvalidStatus", "Status must be pending, processing, failed, or sent", "status")])
            };
        }

        var total = await messages.CountAsync(cancellationToken);
        var items = await messages
            .OrderByDescending(message => message.CreatedAt)
            .Skip(Pagination.GetOffset(page, pageSize))
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new AdminEmailOutboxListResponse(
            items.Select(message => MapEmailOutbox(message, now)).ToArray(),
            total,
            page,
            pageSize);
    }

    public async Task<AdminEmailOutboxDto> RetryEmailAsync(
        ClaimsPrincipal principal,
        Guid messageId,
        AdminRetryEmailRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        var message = await dbContext.EmailOutboxMessages.FirstOrDefaultAsync(item => item.Id == messageId, cancellationToken)
            ?? throw new ApiException("Email outbox message not found", HttpStatusCode.NotFound);

        if (message.SentAt is not null)
        {
            throw new ApiException("A sent email cannot be retried", HttpStatusCode.Conflict);
        }

        if (message.LeaseId is not null && message.LockedUntil > DateTimeOffset.UtcNow)
        {
            throw new ApiException("Email is currently being processed", HttpStatusCode.Conflict);
        }

        message.Attempts = 0;
        message.NextAttemptAt = DateTimeOffset.UtcNow;
        message.LeaseId = null;
        message.LockedUntil = null;
        message.LastError = null;
        message.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actorUserId, "Admin.EmailOutbox.Retry", "EmailOutboxMessage", message.Id, request.Reason ?? message.Template);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ApiException("Email processing state changed; reload and retry", HttpStatusCode.Conflict);
        }

        return MapEmailOutbox(message, DateTimeOffset.UtcNow);
    }

    private string GetEmailOutboxStatus(EmailOutboxMessage message, DateTimeOffset now)
    {
        if (message.SentAt is not null) return "sent";
        if (message.LeaseId is not null && message.LockedUntil > now) return "processing";
        return message.Attempts >= emailOutboxOptions.MaxAttempts ? "failed" : "pending";
    }

    private AdminEmailOutboxDto MapEmailOutbox(EmailOutboxMessage message, DateTimeOffset now)
    {
        return new AdminEmailOutboxDto(
            message.Id,
            message.Recipient,
            message.Template,
            GetEmailOutboxStatus(message, now),
            message.Attempts,
            message.NextAttemptAt,
            message.LockedUntil,
            message.SentAt,
            message.LastError,
            message.CreatedAt);
    }

    private async Task<User> GetUserWithRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new ApiException("User not found", HttpStatusCode.NotFound);
    }

    private async Task RevokeRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > DateTimeOffset.UtcNow)
            .ToArrayAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
        }
    }

    private async Task<AdminProjectDto> UpdateProjectStatusAsync(
        ClaimsPrincipal principal,
        Guid projectId,
        ProjectStatus status,
        string auditAction,
        string reason,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateReason(reason);
        var project = await dbContext.Projects
            .Include(item => item.OwnerUser)
            .FirstOrDefaultAsync(item => item.Id == projectId, cancellationToken)
            ?? throw new ApiException("Project not found", HttpStatusCode.NotFound);
        var setting = await dbContext.ProjectVisibilitySettings.FirstOrDefaultAsync(item => item.ProjectId == project.Id, cancellationToken);

        project.Status = status;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        project.IsRecruiting = status is not (ProjectStatus.Closed or ProjectStatus.Archived or ProjectStatus.Hidden);
        AddAudit(actorUserId, auditAction, "Project", project.Id, reason);
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = MapProject(project, setting);
        await realtimeNotifier.ProjectStatusChangedAsync(project.Id, result, cancellationToken);
        return result;
    }

    private async Task EnsureDefaultSettingsAsync(CancellationToken cancellationToken)
    {
        var existingSettings = await dbContext.SystemSettings.ToDictionaryAsync(setting => setting.Key, cancellationToken);
        foreach (var definition in GetDefaultSettings())
        {
            if (existingSettings.TryGetValue(definition.Key, out var existing))
            {
                existing.Group = definition.Group;
                existing.Type = definition.Type;
                existing.IsReadonly = definition.IsReadonly;
                continue;
            }

            dbContext.SystemSettings.Add(new SystemSetting
            {
                Key = definition.Key,
                Group = definition.Group,
                Value = definition.Value,
                Type = definition.Type,
                IsReadonly = definition.IsReadonly
            });
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_system_settings_Key"
        })
        {
            // Another API instance seeded the same defaults concurrently.
            dbContext.ChangeTracker.Clear();
        }
    }

    private IReadOnlyCollection<SystemSetting> GetDefaultSettings()
    {
        return
        [
            new SystemSetting { Key = "AI.DailyQuota", Group = "AI", Value = configuration["AI:DailyQuota"] ?? "20", Type = "number" },
            new SystemSetting { Key = "FileStorage.MaxCvBytes", Group = "FileStorage", Value = configuration["FileStorage:MaxCvBytes"] ?? "5242880", Type = "number" },
            new SystemSetting { Key = "Realtime.Enabled", Group = "Realtime", Value = "true", Type = "boolean" },
            new SystemSetting { Key = "Moderation.Policy", Group = "Moderation", Value = "manual_review_required", Type = "string", IsReadonly = true },
            new SystemSetting { Key = "Subscriptions.Enabled", Group = "Subscriptions", Value = "true", Type = "boolean" },
            new SystemSetting { Key = "Payments.Provider", Group = "Payments", Value = configuration["Payments:Provider"] ?? "Mock", Type = "string", IsReadonly = true },
            new SystemSetting { Key = "Payments.CheckoutEnabled", Group = "Payments", Value = "true", Type = "boolean" },
            new SystemSetting { Key = "Email.Provider", Group = "Email", Value = configuration["Email:Provider"] ?? "Development", Type = "string", IsReadonly = true },
            new SystemSetting { Key = "Email.Enabled", Group = "Email", Value = "true", Type = "boolean" },
            new SystemSetting { Key = "Notifications.Enabled", Group = "Notifications", Value = "true", Type = "boolean" }
        ];
    }

    private void ValidateSettingValue(SystemSetting setting, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException([new ErrorDetail("Required", "Setting value is required", "value")]);
        }

        if (setting.Type == "number" && !decimal.TryParse(value, out _))
        {
            throw new ValidationException([new ErrorDetail("InvalidNumber", "Setting value must be a number", "value")]);
        }

        if (setting.Type == "boolean" && !bool.TryParse(value, out _))
        {
            throw new ValidationException([new ErrorDetail("InvalidBoolean", "Setting value must be true or false", "value")]);
        }

        if (value.Trim().Length > 2000)
        {
            throw new ValidationException([new ErrorDetail("ValueTooLong", "Setting value must be at most 2000 characters", "value")]);
        }

        if (setting.Key == "AI.DailyQuota" &&
            (!int.TryParse(value, out var dailyQuota) || dailyQuota is < 1 or > 10_000))
        {
            throw new ValidationException([new ErrorDetail("InvalidQuota", "AI daily quota must be between 1 and 10000", "value")]);
        }

        var maxRequestBodySize = long.TryParse(configuration["Security:MaxRequestBodySizeBytes"], out var configuredRequestLimit)
            ? configuredRequestLimit
            : 25 * 1024 * 1024;
        if (setting.Key == "FileStorage.MaxCvBytes" &&
            (!long.TryParse(value, out var maxCvBytes) || maxCvBytes < 4 || maxCvBytes > maxRequestBodySize))
        {
            throw new ValidationException([new ErrorDetail("InvalidFileLimit", $"CV upload limit must be between 4 bytes and the request limit of {maxRequestBodySize} bytes", "value")]);
        }
    }

    private async Task EnsurePlanExistsAsync(Guid planId, CancellationToken cancellationToken)
    {
        if (!await dbContext.SubscriptionPlans.AnyAsync(plan => plan.Id == planId, cancellationToken))
        {
            throw new ApiException("Subscription plan not found", HttpStatusCode.NotFound);
        }
    }

    private static void ValidatePlanRequest(AdminSubscriptionPlanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ValidationException([new ErrorDetail("Required", "Plan code is required", "code")]);
        }

        if (request.Code.Trim().Length > 80)
        {
            throw new ValidationException([new ErrorDetail("CodeTooLong", "Plan code must be at most 80 characters", "code")]);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException([new ErrorDetail("Required", "Plan name is required", "name")]);
        }

        if (request.Name.Trim().Length > 160)
        {
            throw new ValidationException([new ErrorDetail("NameTooLong", "Plan name must be at most 160 characters", "name")]);
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ValidationException([new ErrorDetail("Required", "Plan description is required", "description")]);
        }

        if (request.Description.Trim().Length > 1000)
        {
            throw new ValidationException([new ErrorDetail("DescriptionTooLong", "Plan description must be at most 1000 characters", "description")]);
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Trim().Length != 3)
        {
            throw new ValidationException([new ErrorDetail("InvalidCurrency", "Currency must be a 3-letter code", "currency")]);
        }

        if (request.MonthlyPrice < 0)
        {
            throw new ValidationException([new ErrorDetail("InvalidPrice", "Monthly price must be greater than or equal to 0", "monthlyPrice")]);
        }
    }

    private static void ValidateQuotaRequest(AdminUsageQuotaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ResourceKey))
        {
            throw new ValidationException([new ErrorDetail("Required", "Resource key is required", "resourceKey")]);
        }

        if (request.ResourceKey.Trim().Length > 120)
        {
            throw new ValidationException([new ErrorDetail("ResourceKeyTooLong", "Resource key must be at most 120 characters", "resourceKey")]);
        }

        if (request.Limit < 0)
        {
            throw new ValidationException([new ErrorDetail("InvalidLimit", "Quota limit must be greater than or equal to 0", "limit")]);
        }
    }

    private static AdminSettingDto MapSetting(SystemSetting setting)
    {
        return new AdminSettingDto(
            setting.Key,
            setting.Group,
            setting.Value,
            setting.Type,
            setting.IsReadonly,
            setting.UpdatedAt);
    }

    private static AdminProjectDto MapProject(Project project, ProjectVisibilitySetting? setting)
    {
        return new AdminProjectDto(
            project.Id,
            project.Title,
            project.Slug,
            project.Summary,
            project.OwnerUserId,
            project.OwnerUser.Email,
            project.OwnerUser.FullName,
            project.Status,
            setting?.Visibility ?? ProjectVisibility.Private,
            project.Stage,
            project.IsRecruiting,
            project.IsDeleted,
            project.CreatedAt,
            project.UpdatedAt);
    }

    private static AdminSubscriptionPlanDto MapPlan(SubscriptionPlan plan, IReadOnlyCollection<UsageQuota> quotas)
    {
        return new AdminSubscriptionPlanDto(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.Description,
            plan.MonthlyPrice,
            plan.Currency,
            plan.IsActive,
            quotas.Select(MapQuota).ToArray(),
            plan.CreatedAt,
            plan.UpdatedAt);
    }

    private static AdminUsageQuotaDto MapQuota(UsageQuota quota)
    {
        return new AdminUsageQuotaDto(
            quota.Id,
            quota.ResourceKey,
            quota.Limit,
            quota.CreatedAt,
            quota.UpdatedAt);
    }

    private void AddAudit(Guid actorUserId, string action, string resourceType, Guid resourceId, string reason)
    {
        var normalizedReason = reason.Trim();
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Reason = normalizedReason[..Math.Min(normalizedReason.Length, 500)]
        });
    }

    private static AdminUserDto MapUser(User user)
    {
        return new AdminUserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.IsEmailVerified,
            user.Status,
            user.IsSuspended,
            user.SuspendedUntil,
            user.SuspensionReason,
            user.BannedAt,
            user.BanReason,
            user.IsDeleted,
            user.LastLoginAt,
            user.CreatedAt,
            user.UserRoles.Select(userRole => userRole.Role.Code).Order().ToArray());
    }

    private static void EnsureNotSelf(Guid actorUserId, Guid userId, string message)
    {
        if (actorUserId == userId)
        {
            throw new ApiException(message, HttpStatusCode.BadRequest);
        }
    }

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException([new ErrorDetail("Required", "Reason is required", "reason")]);
        }

        if (reason.Trim().Length > 500)
        {
            throw new ValidationException([new ErrorDetail("ReasonTooLong", "Reason must be at most 500 characters", "reason")]);
        }
    }

    private static void ValidateRoleCode(string roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            throw new ValidationException([new ErrorDetail("Required", "Role code is required", "roleCode")]);
        }

        if (roleCode.Trim().Length > 80)
        {
            throw new ValidationException([new ErrorDetail("RoleCodeTooLong", "Role code must be at most 80 characters", "roleCode")]);
        }
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
