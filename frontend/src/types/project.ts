import { ProjectStatus, ProjectStage, ProjectVisibility } from './enums';

export interface ProjectSummaryDto {
  id: string;
  title: string;
  slug: string;
  summary: string;
  status: ProjectStatus;
  stage: ProjectStage;
  visibility: ProjectVisibility;
  isRecruiting: boolean;
  createdAt: string;
}

export interface ProjectDetailDto {
  id: string;
  ownerUserId: string;
  title: string;
  slug: string;
  summary: string;
  problem: string;
  solution: string;
  targetMarket?: string;
  businessModel?: string;
  fundingNeeds?: string;
  pitchDeckUrl?: string;
  status: ProjectStatus;
  stage: ProjectStage;
  isRecruiting: boolean;
  visibility: ProjectVisibility;
  requiresNda: boolean;
  requiredRoles: ProjectRequiredRoleDto[];
  requiredSkills: ProjectSkillDto[];
  createdAt: string;
  updatedAt?: string;
}

export interface CreateProjectDraftRequest {
  title: string;
  summary: string;
  problem: string;
  solution: string;
  stage: ProjectStage;
  visibility: ProjectVisibility;
}

export interface UpdateProjectRequest {
  title: string;
  summary: string;
  problem: string;
  solution: string;
  targetMarket?: string;
  businessModel?: string;
  fundingNeeds?: string;
  pitchDeckUrl?: string;
  stage: ProjectStage;
  visibility: ProjectVisibility;
  isRecruiting: boolean;
  requiredRoles: UpsertProjectRequiredRoleDto[];
  requiredSkillIds: string[];
}

export interface UpsertProjectRequiredRoleDto {
  roleName: string;
  description?: string;
  slots: number;
  isOpen: boolean;
}

export interface ProjectRequiredRoleDto {
  id: string;
  roleName: string;
  description?: string;
  slots: number;
  isOpen: boolean;
}

export interface ProjectSkillDto {
  id: string;
  name: string;
}

export interface ProjectVersionDto {
  id: string;
  versionNumber: number;
  changeReason: string;
  createdAt: string;
}

export interface LandingProject {
  id: string;
  title: string;
  summary: string;
  field: string;
  stage: string;
  status: string;
  roles: string[];
  memberCount: number;
  seekingInvestor: boolean;
  href: string;
}
