import type { ProjectStage, ProjectVisibility } from "./enums";

export interface RecommendationBreakdownItemDto {
  key: string;
  points: number;
  explanation: string;
}

export interface ProjectRecommendationDto {
  recommendationId: string;
  projectId: string;
  title: string;
  summary: string;
  stage: ProjectStage;
  visibility: ProjectVisibility;
  score: number;
  breakdown: RecommendationBreakdownItemDto[];
}

export interface MemberRecommendationDto {
  recommendationId: string;
  projectId: string;
  projectTitle: string;
  userId: string;
  fullName: string;
  headline: string;
  location?: string;
  matchedSkills: string[];
  score: number;
  breakdown: RecommendationBreakdownItemDto[];
}

export interface RecommendationListResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
