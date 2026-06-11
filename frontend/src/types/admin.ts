// Note: Backend admin endpoints are not yet implemented.
// These types are prepared for future integration.

export interface AdminDashboardDto {
  totalUsers: number;
  totalProjects: number;
  pendingProjects: number;
  publishedProjects: number;
  openReports: number;
  totalInvestorInterests: number;
  totalApplications: number;
}

export interface AdminUserDto {
  id: string;
  email: string;
  fullName: string;
  isEmailVerified: boolean;
  isSuspended: boolean;
  roles: string[];
  createdAt: string;
  lastLoginAt?: string;
}

export interface AuditLogDto {
  id: string;
  actorUserId?: string;
  action: string;
  resourceType: string;
  resourceId?: string;
  reason?: string;
  metadataJson?: string;
  ipAddress?: string;
  userAgent?: string;
  createdAt: string;
}

export interface AssignRoleRequest {
  roleCode: string;
}
