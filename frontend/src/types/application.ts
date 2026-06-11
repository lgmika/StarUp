import { ApplicationStatus } from './enums';

export interface ApplyProjectRequest {
  cvId?: string;
  coverLetter: string;
}

export interface ApplicationDto {
  id: string;
  projectId: string;
  projectTitle: string;
  applicantUserId: string;
  applicantEmail: string;
  applicantFullName: string;
  cvId?: string;
  cvTitle?: string;
  coverLetter: string;
  status: ApplicationStatus;
  founderNote?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface ApplicationStatusHistoryDto {
  id: string;
  fromStatus: ApplicationStatus;
  toStatus: ApplicationStatus;
  changedByUserId: string;
  reason?: string;
  createdAt: string;
}

export interface ApplicationDetailDto {
  application: ApplicationDto;
  statusHistory: ApplicationStatusHistoryDto[];
}

export interface ApplicationDecisionRequest {
  reason?: string;
  founderNote?: string;
}
