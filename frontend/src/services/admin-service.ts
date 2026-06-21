import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type {
  AdminAuditLogListResponse,
  AdminDashboardDto,
  AdminProjectDto,
  AdminProjectListResponse,
  AdminRoleDto,
  AdminRoleRequest,
  AdminUserDto,
  AdminUserListResponse,
  AdminUserStatusRequest,
  AdminSettingDto,
  AdminSubscriptionPlanDto,
  AdminSubscriptionPlanRequest,
  AdminUsageQuotaDto,
  AdminUsageQuotaRequest,
  AdminEmailOutboxDto,
  AdminEmailOutboxListResponse,
} from "@/types/admin";
import type { ProjectStage, ProjectStatus, ProjectVisibility } from "@/types/enums";

interface AdminUserQuery {
  search?: string;
  status?: string;
  roleCode?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminProjectQuery {
  search?: string;
  status?: ProjectStatus;
  stage?: ProjectStage;
  visibility?: ProjectVisibility;
  ownerEmail?: string;
  page?: number;
  pageSize?: number;
}

export const adminService = {
  async getDashboard() {
    const { data } = await api.get<ApiResponse<AdminDashboardDto>>("/admin/dashboard");
    return data.data;
  },

  async listUsers(params: AdminUserQuery = {}) {
    const { data } = await api.get<ApiResponse<AdminUserListResponse>>("/admin/users", { params });
    return data.data;
  },

  async getUser(userId: string) {
    const { data } = await api.get<ApiResponse<AdminUserDto>>(`/admin/users/${userId}`);
    return data.data;
  },

  async suspendUser(userId: string, request: AdminUserStatusRequest) {
    const { data } = await api.post<ApiResponse<AdminUserDto>>(`/admin/users/${userId}/suspend`, request);
    return data.data;
  },

  async unsuspendUser(userId: string, request: AdminUserStatusRequest) {
    const { data } = await api.post<ApiResponse<AdminUserDto>>(`/admin/users/${userId}/unsuspend`, request);
    return data.data;
  },

  async banUser(userId: string, request: AdminUserStatusRequest) {
    const { data } = await api.post<ApiResponse<AdminUserDto>>(`/admin/users/${userId}/ban`, request);
    return data.data;
  },

  async unbanUser(userId: string, request: AdminUserStatusRequest) {
    const { data } = await api.post<ApiResponse<AdminUserDto>>(`/admin/users/${userId}/unban`, request);
    return data.data;
  },

  async addRole(userId: string, request: AdminRoleRequest) {
    const { data } = await api.post<ApiResponse<AdminUserDto>>(`/admin/users/${userId}/roles`, request);
    return data.data;
  },

  async removeRole(userId: string, roleCode: string) {
    const { data } = await api.delete<ApiResponse<AdminUserDto>>(`/admin/users/${userId}/roles/${roleCode}`);
    return data.data;
  },

  async listRoles() {
    const { data } = await api.get<ApiResponse<AdminRoleDto[]>>("/admin/roles");
    return data.data;
  },

  async listAuditLogs(page = 1, pageSize = 20) {
    const { data } = await api.get<ApiResponse<AdminAuditLogListResponse>>("/admin/audit-logs", {
      params: { page, pageSize },
    });
    return data.data;
  },

  async listSettings() {
    const { data } = await api.get<ApiResponse<AdminSettingDto[]>>("/admin/settings");
    return data.data;
  },

  async updateSetting(key: string, value: string, reason?: string) {
    const { data } = await api.put<ApiResponse<AdminSettingDto>>(`/admin/settings/${encodeURIComponent(key)}`, { value, reason });
    return data.data;
  },

  async listProjects(params: AdminProjectQuery = {}) {
    const { data } = await api.get<ApiResponse<AdminProjectListResponse>>("/admin/projects", { params });
    return data.data;
  },

  async projectAction(projectId: string, action: "hide" | "restore" | "archive" | "close", reason: string) {
    const { data } = await api.post<ApiResponse<AdminProjectDto>>(`/admin/projects/${projectId}/${action}`, { reason });
    return data.data;
  },

  async forceProjectStatus(projectId: string, status: ProjectStatus, reason: string) {
    const { data } = await api.post<ApiResponse<AdminProjectDto>>(`/admin/projects/${projectId}/status`, { status, reason });
    return data.data;
  },

  async listSubscriptionPlans() {
    const { data } = await api.get<ApiResponse<AdminSubscriptionPlanDto[]>>("/admin/subscription-plans");
    return data.data;
  },

  async createSubscriptionPlan(request: AdminSubscriptionPlanRequest) {
    const { data } = await api.post<ApiResponse<AdminSubscriptionPlanDto>>("/admin/subscription-plans", request);
    return data.data;
  },

  async updateSubscriptionPlan(planId: string, request: AdminSubscriptionPlanRequest) {
    const { data } = await api.put<ApiResponse<AdminSubscriptionPlanDto>>(`/admin/subscription-plans/${planId}`, request);
    return data.data;
  },

  async createUsageQuota(planId: string, request: AdminUsageQuotaRequest) {
    const { data } = await api.post<ApiResponse<AdminUsageQuotaDto>>(`/admin/subscription-plans/${planId}/quotas`, request);
    return data.data;
  },

  async updateUsageQuota(planId: string, quotaId: string, request: AdminUsageQuotaRequest) {
    const { data } = await api.put<ApiResponse<AdminUsageQuotaDto>>(`/admin/subscription-plans/${planId}/quotas/${quotaId}`, request);
    return data.data;
  },

  async deleteUsageQuota(planId: string, quotaId: string, reason?: string) {
    await api.delete<ApiResponse<null>>(`/admin/subscription-plans/${planId}/quotas/${quotaId}`, { params: { reason } });
  },

  async listEmailOutbox(params: { status?: string; recipient?: string; page?: number; pageSize?: number } = {}) {
    const { data } = await api.get<ApiResponse<AdminEmailOutboxListResponse>>("/admin/email-outbox", { params });
    return data.data;
  },

  async retryEmail(messageId: string, reason?: string) {
    const { data } = await api.post<ApiResponse<AdminEmailOutboxDto>>(`/admin/email-outbox/${messageId}/retry`, { reason });
    return data.data;
  },
};
