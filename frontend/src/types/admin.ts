export interface AdminDashboardDto {
  totalUsers: number;
  newUsersLast7Days: number;
  activeUsers: number;
  verifiedUsers: number;
  totalProjects: number;
  pendingModeration: number;
  openReports: number;
  applications: number;
  investors: number;
  aiRequests: number;
  storageBytes: number;
}

export interface AdminUserDto {
  id: string;
  email: string;
  fullName: string;
  isEmailVerified: boolean;
  status: string;
  isSuspended: boolean;
  suspendedUntil?: string | null;
  suspensionReason?: string | null;
  bannedAt?: string | null;
  banReason?: string | null;
  isDeleted: boolean;
  lastLoginAt?: string | null;
  roles: string[];
  createdAt: string;
}

export interface AdminUserListResponse {
  items: AdminUserDto[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AdminRoleDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
}

export interface AdminAuditLogDto {
  id: string;
  actorUserId?: string | null;
  action: string;
  resourceType: string;
  resourceId?: string | null;
  reason?: string | null;
  metadataJson?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  createdAt: string;
}

export interface AdminAuditLogListResponse {
  items: AdminAuditLogDto[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AdminUserStatusRequest {
  reason: string;
  suspendedUntil?: string | null;
}

export interface AdminRoleRequest {
  roleCode: string;
}

export type AuditLogDto = AdminAuditLogDto;
export type AssignRoleRequest = AdminRoleRequest;
