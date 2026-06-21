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

export interface AdminSettingDto {
  key: string;
  group: string;
  value: string;
  type: string;
  isReadonly: boolean;
  updatedAt?: string | null;
}

export interface AdminProjectDto {
  id: string;
  title: string;
  slug: string;
  summary: string;
  ownerUserId: string;
  ownerEmail: string;
  ownerFullName: string;
  status: import("./enums").ProjectStatus;
  visibility: import("./enums").ProjectVisibility;
  stage: import("./enums").ProjectStage;
  isRecruiting: boolean;
  isDeleted: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface AdminProjectListResponse {
  items: AdminProjectDto[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AdminUsageQuotaDto {
  id: string;
  resourceKey: string;
  limit: number;
  createdAt: string;
  updatedAt?: string | null;
}

export interface AdminSubscriptionPlanDto {
  id: string;
  code: string;
  name: string;
  description: string;
  monthlyPrice: number;
  currency: string;
  isActive: boolean;
  quotas: AdminUsageQuotaDto[];
  createdAt: string;
  updatedAt?: string | null;
}

export interface AdminSubscriptionPlanRequest {
  code: string;
  name: string;
  description: string;
  monthlyPrice: number;
  currency: string;
  isActive: boolean;
  reason?: string;
}

export interface AdminUsageQuotaRequest {
  resourceKey: string;
  limit: number;
  reason?: string;
}

export interface AdminEmailOutboxDto {
  id: string;
  recipient: string;
  template: string;
  status: string;
  attempts: number;
  nextAttemptAt: string;
  lockedUntil?: string | null;
  sentAt?: string | null;
  lastError?: string | null;
  createdAt: string;
}

export interface AdminEmailOutboxListResponse {
  items: AdminEmailOutboxDto[];
  total: number;
  page: number;
  pageSize: number;
}

export type AuditLogDto = AdminAuditLogDto;
export type AssignRoleRequest = AdminRoleRequest;
