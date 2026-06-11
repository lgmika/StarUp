import { ModerationDecision, ProjectStatus, ProjectStage } from './enums';

export interface ModeratorDashboardDto {
  pendingProjects: number;
  publishedProjects: number;
  rejectedProjects: number;
  hiddenProjects: number;
  pendingReports: number;
}

export interface ModeratorProjectQueueItemDto {
  projectId: string;
  title: string;
  summary: string;
  status: ProjectStatus;
  stage: ProjectStage;
  latestAIQualityScore?: number;
  latestAIRiskFlags: string[];
  submittedAt?: string;
}

export interface ModeratorProjectDetailDto {
  projectId: string;
  title: string;
  summary: string;
  problem: string;
  solution: string;
  status: ProjectStatus;
  stage: ProjectStage;
  ownerUserId: string;
  ownerEmail: string;
  latestAIQualityScore?: number;
  latestAIRiskFlags: string[];
  moderationHistory: ModerationReviewDto[];
}

export interface ModerationReviewDto {
  id: string;
  decision: ModerationDecision;
  reason: string;
  aiQualityScoreSnapshot?: number;
  createdAt: string;
}

export interface ModerationDecisionRequest {
  reason: string;
}
