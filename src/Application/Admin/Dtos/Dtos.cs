using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Admin.Dtos;

public sealed record AdminDashboardDto(
    int TotalUsers,
    int NewUsersLast7Days,
    int ActiveUsers,
    int VerifiedUsers,
    int TotalProjects,
    int PendingModeration,
    int OpenReports,
    int Applications,
    int Investors,
    int AIRequests,
    long StorageBytes);

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string FullName,
    bool IsEmailVerified,
    UserStatus Status,
    bool IsSuspended,
    DateTimeOffset? SuspendedUntil,
    string? SuspensionReason,
    DateTimeOffset? BannedAt,
    string? BanReason,
    bool IsDeleted,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> Roles);

public sealed record AdminUserListResponse(
    IReadOnlyCollection<AdminUserDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record AdminUserQuery(
    string? Search = null,
    UserStatus? Status = null,
    string? RoleCode = null,
    int Page = 1,
    int PageSize = 20);

public sealed record AdminUserStatusRequest(
    string Reason,
    DateTimeOffset? SuspendedUntil = null);

public sealed record AdminRoleRequest(string RoleCode);

public sealed record AdminRoleDto(
    Guid Id,
    string Code,
    string Name,
    string? Description);

public sealed record AdminAuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string Action,
    string ResourceType,
    Guid? ResourceId,
    string? Reason,
    string? MetadataJson,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset CreatedAt);

public sealed record AdminAuditLogListResponse(
    IReadOnlyCollection<AdminAuditLogDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record AdminSettingDto(
    string Key,
    string Group,
    string Value,
    string Type,
    bool IsReadonly,
    DateTimeOffset? UpdatedAt);

public sealed record UpdateAdminSettingRequest(string Value, string? Reason = null);

public sealed record AdminProjectDto(
    Guid Id,
    string Title,
    string Slug,
    string Summary,
    Guid OwnerUserId,
    string OwnerEmail,
    string OwnerFullName,
    ProjectStatus Status,
    ProjectVisibility Visibility,
    ProjectStage Stage,
    bool IsRecruiting,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record AdminProjectListResponse(
    IReadOnlyCollection<AdminProjectDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record AdminProjectQuery(
    string? Search = null,
    ProjectStatus? Status = null,
    ProjectStage? Stage = null,
    ProjectVisibility? Visibility = null,
    string? OwnerEmail = null,
    int Page = 1,
    int PageSize = 20);

public sealed record AdminProjectActionRequest(string Reason);

public sealed record AdminProjectStatusRequest(ProjectStatus Status, string Reason);

public sealed record AdminSubscriptionPlanRequest(
    string Code,
    string Name,
    string Description,
    decimal MonthlyPrice,
    string Currency,
    bool IsActive,
    string? Reason = null);

public sealed record AdminSubscriptionPlanDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    decimal MonthlyPrice,
    string Currency,
    bool IsActive,
    IReadOnlyCollection<AdminUsageQuotaDto> Quotas,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record AdminUsageQuotaDto(
    Guid Id,
    string ResourceKey,
    int Limit,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record AdminUsageQuotaRequest(
    string ResourceKey,
    int Limit,
    string? Reason = null);

public sealed record AdminEmailOutboxDto(
    Guid Id,
    string Recipient,
    string Template,
    string Status,
    int Attempts,
    DateTimeOffset NextAttemptAt,
    DateTimeOffset? LockedUntil,
    DateTimeOffset? SentAt,
    string? LastError,
    DateTimeOffset CreatedAt);

public sealed record AdminEmailOutboxListResponse(
    IReadOnlyCollection<AdminEmailOutboxDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record AdminEmailOutboxQuery(
    string? Status = null,
    string? Recipient = null,
    int Page = 1,
    int PageSize = 20);

public sealed record AdminRetryEmailRequest(string? Reason = null);
