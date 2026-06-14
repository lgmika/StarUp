import type { ProjectStage, ProjectStatus, ProjectVisibility } from "./enums";

export interface SearchResultPage<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ProjectSearchItemDto {
  id: string;
  title: string;
  slug: string;
  summary: string;
  status: ProjectStatus;
  stage: ProjectStage;
  visibility: ProjectVisibility;
  isRecruiting: boolean;
  createdAt: string;
  rank: number;
}

export interface MemberSearchItemDto {
  userId: string;
  fullName: string;
  headline: string;
  location?: string;
  skills: string[];
  rank: number;
}

export interface InvestorSearchItemDto {
  userId: string;
  displayName: string;
  organizationName?: string;
  investmentFocus?: string;
  minTicketSize?: number;
  maxTicketSize?: number;
  rank: number;
}

export interface SearchSuggestionDto {
  type: string;
  id?: string;
  label: string;
  description?: string;
}

export interface SearchSuggestionsResponse {
  items: SearchSuggestionDto[];
}

export interface ProjectSearchParams {
  keyword?: string;
  status?: ProjectStatus;
  stage?: ProjectStage;
  requiredRole?: string;
  requiredSkillId?: string;
  location?: string;
  remote?: boolean;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export interface MemberSearchParams {
  keyword?: string;
  skillId?: string;
  minYearsOfExperience?: number;
  location?: string;
  verifiedOnly?: boolean;
  page?: number;
  pageSize?: number;
}

export interface InvestorSearchParams {
  keyword?: string;
  minTicketSize?: number;
  maxTicketSize?: number;
  page?: number;
  pageSize?: number;
}
