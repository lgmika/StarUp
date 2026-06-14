import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type {
  AdminAuditLogListResponse,
  AdminDashboardDto,
  AdminRoleDto,
  AdminRoleRequest,
  AdminUserDto,
  AdminUserListResponse,
  AdminUserStatusRequest,
} from "@/types/admin";

interface AdminUserQuery {
  search?: string;
  status?: string;
  roleCode?: string;
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
};
