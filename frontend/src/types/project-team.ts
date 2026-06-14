import type { ProjectMemberRole } from "./enums";

export interface ProjectMemberDto {
  id: string;
  projectId: string;
  userId: string;
  email: string;
  fullName: string;
  role: ProjectMemberRole;
  joinedAt: string;
  isActive: boolean;
}

export interface UpdateProjectMemberRequest {
  role: ProjectMemberRole;
  reason: string;
}

export interface CreateProjectInvitationRequest {
  email: string;
  role: ProjectMemberRole;
  message?: string;
}

export interface ProjectInvitationDto {
  id: string;
  projectId: string;
  invitedByUserId: string;
  invitedUserId?: string;
  email: string;
  role: ProjectMemberRole;
  message?: string;
  status: string;
  expiresAt: string;
  createdAt: string;
  respondedAt?: string;
}

export interface TransferOwnershipRequest {
  toUserId: string;
  reason: string;
}
