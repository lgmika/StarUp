using System.Security.Claims;
using StartupConnect.Application.Admin.Dtos;

namespace StartupConnect.Application.Admin.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken);

    Task<AdminUserListResponse> GetUsersAsync(AdminUserQuery query, CancellationToken cancellationToken);

    Task<AdminUserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<AdminUserDto> SuspendUserAsync(ClaimsPrincipal principal, Guid userId, AdminUserStatusRequest request, CancellationToken cancellationToken);

    Task<AdminUserDto> UnsuspendUserAsync(ClaimsPrincipal principal, Guid userId, AdminUserStatusRequest request, CancellationToken cancellationToken);

    Task<AdminUserDto> BanUserAsync(ClaimsPrincipal principal, Guid userId, AdminUserStatusRequest request, CancellationToken cancellationToken);

    Task<AdminUserDto> UnbanUserAsync(ClaimsPrincipal principal, Guid userId, AdminUserStatusRequest request, CancellationToken cancellationToken);

    Task<AdminUserDto> AddRoleAsync(ClaimsPrincipal principal, Guid userId, AdminRoleRequest request, CancellationToken cancellationToken);

    Task<AdminUserDto> RemoveRoleAsync(ClaimsPrincipal principal, Guid userId, string roleCode, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdminRoleDto>> GetRolesAsync(CancellationToken cancellationToken);

    Task<AdminAuditLogListResponse> GetAuditLogsAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdminSettingDto>> GetSettingsAsync(CancellationToken cancellationToken);

    Task<AdminSettingDto> UpdateSettingAsync(ClaimsPrincipal principal, string key, UpdateAdminSettingRequest request, CancellationToken cancellationToken);

    Task<AdminProjectListResponse> GetProjectsAsync(AdminProjectQuery query, CancellationToken cancellationToken);

    Task<AdminProjectDto> HideProjectAsync(ClaimsPrincipal principal, Guid projectId, AdminProjectActionRequest request, CancellationToken cancellationToken);

    Task<AdminProjectDto> RestoreProjectAsync(ClaimsPrincipal principal, Guid projectId, AdminProjectActionRequest request, CancellationToken cancellationToken);

    Task<AdminProjectDto> ArchiveProjectAsync(ClaimsPrincipal principal, Guid projectId, AdminProjectActionRequest request, CancellationToken cancellationToken);

    Task<AdminProjectDto> CloseProjectAsync(ClaimsPrincipal principal, Guid projectId, AdminProjectActionRequest request, CancellationToken cancellationToken);

    Task<AdminProjectDto> ForceProjectStatusAsync(ClaimsPrincipal principal, Guid projectId, AdminProjectStatusRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdminSubscriptionPlanDto>> GetSubscriptionPlansAsync(CancellationToken cancellationToken);

    Task<AdminSubscriptionPlanDto> CreateSubscriptionPlanAsync(ClaimsPrincipal principal, AdminSubscriptionPlanRequest request, CancellationToken cancellationToken);

    Task<AdminSubscriptionPlanDto> UpdateSubscriptionPlanAsync(ClaimsPrincipal principal, Guid planId, AdminSubscriptionPlanRequest request, CancellationToken cancellationToken);

    Task<AdminUsageQuotaDto> CreateUsageQuotaAsync(ClaimsPrincipal principal, Guid planId, AdminUsageQuotaRequest request, CancellationToken cancellationToken);

    Task<AdminUsageQuotaDto> UpdateUsageQuotaAsync(ClaimsPrincipal principal, Guid planId, Guid quotaId, AdminUsageQuotaRequest request, CancellationToken cancellationToken);

    Task DeleteUsageQuotaAsync(ClaimsPrincipal principal, Guid planId, Guid quotaId, string? reason, CancellationToken cancellationToken);
}
