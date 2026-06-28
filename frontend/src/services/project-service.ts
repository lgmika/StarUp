import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type {
  CreateProjectDraftRequest,
  ProjectDetailDto,
  ProjectSummaryDto,
  ProjectVersionDto,
  UpdateProjectRequest,
} from "@/types/project";
import type { AIRecommendationDto, AIReviewDto, ApplyAIRecommendationResponse } from "@/types/ai";
import type { ActivityListResponse } from "@/types/activity";
import type { FounderProjectDashboardDto } from "@/types/dashboard";
import type { MemberRecommendationDto, RecommendationListResponse } from "@/types/recommendation";
import type { AITextResponse } from "@/types/ai";

export const projectService = {
  async listProjects(search?: string) {
    const keyword = search?.trim();
    const { data } = await api.get<ApiResponse<ProjectSummaryDto[]>>("/projects", {
      params: keyword ? { search: keyword } : undefined,
      allowAnonymousFallback: true,
    });
    return data.data;
  },

  async getProject(projectId: string) {
    const { data } = await api.get<ApiResponse<ProjectDetailDto>>(`/projects/${projectId}`, {
      skipForbiddenRedirect: true,
      allowAnonymousFallback: true,
    });
    return data.data;
  },

  async createDraft(request: CreateProjectDraftRequest) {
    const { data } = await api.post<ApiResponse<ProjectDetailDto>>("/projects/drafts", request);
    return data.data;
  },

  async updateProject(projectId: string, request: UpdateProjectRequest) {
    const { data } = await api.put<ApiResponse<ProjectDetailDto>>(`/projects/${projectId}`, request);
    return data.data;
  },

  async listOwnedProjects() {
    const { data } = await api.get<ApiResponse<ProjectSummaryDto[]>>("/projects/me/owned");
    return data.data;
  },

  async listJoinedProjects() {
    const { data } = await api.get<ApiResponse<ProjectSummaryDto[]>>("/projects/me/joined");
    return data.data;
  },

  async listSavedProjects() {
    const { data } = await api.get<ApiResponse<ProjectSummaryDto[]>>("/users/me/saved-projects");
    return data.data;
  },

  async saveProject(projectId: string) {
    await api.post<ApiResponse<null>>(`/projects/${projectId}/save`);
  },

  async unsaveProject(projectId: string) {
    await api.delete<ApiResponse<null>>(`/projects/${projectId}/save`);
  },

  async getProjectVersions(projectId: string) {
    const { data } = await api.get<ApiResponse<ProjectVersionDto[]>>(`/projects/${projectId}/versions`, {
      skipForbiddenRedirect: true,
    });
    return data.data;
  },

  async getProjectActivities(projectId: string, page = 1, pageSize = 20) {
    const { data } = await api.get<ApiResponse<ActivityListResponse>>(`/projects/${projectId}/activities`, {
      params: { page, pageSize },
      allowAnonymousFallback: true,
    });
    return data.data;
  },

  async getProjectDashboard(projectId: string) {
    const { data } = await api.get<ApiResponse<FounderProjectDashboardDto>>(`/projects/${projectId}/dashboard`, {
      params: { timezoneOffsetMinutes: new Date().getTimezoneOffset() },
    });
    return data.data;
  },

  async getRecommendedMembers(projectId: string, page = 1, pageSize = 20) {
    const { data } = await api.get<ApiResponse<RecommendationListResponse<MemberRecommendationDto>>>(`/projects/${projectId}/recommended-members`, { params: { page, pageSize } });
    return data.data;
  },

  async getAiReviews(projectId: string) {
    const { data } = await api.get<ApiResponse<AIReviewDto[]>>(`/projects/${projectId}/ai/reviews`);
    return data.data;
  },

  async getInvestorSummary(projectId: string) {
    const { data } = await api.get<ApiResponse<AITextResponse>>(`/projects/${projectId}/investor-summary`);
    return data.data;
  },

  async submitReview(projectId: string) {
    await api.post<ApiResponse<null>>(`/projects/${projectId}/submit-review`);
  },

  async closeProject(projectId: string) {
    await api.post<ApiResponse<null>>(`/projects/${projectId}/close`);
  },

  async archiveProject(projectId: string) {
    await api.delete<ApiResponse<null>>(`/projects/${projectId}`);
  },

  async createAiSuggestions(projectId: string) {
    const { data } = await api.post<ApiResponse<AIRecommendationDto[]>>(`/projects/${projectId}/ai/suggestions`);
    return data.data;
  },

  async createAiReview(projectId: string) {
    const { data } = await api.post<ApiResponse<AIReviewDto>>(`/projects/${projectId}/ai/review`);
    return data.data;
  },

  async getLatestAiReview(projectId: string) {
    const { data } = await api.get<ApiResponse<AIReviewDto>>(`/projects/${projectId}/ai/reviews/latest`, {
      skipForbiddenRedirect: true,
    });
    return data.data;
  },

  async applyAiRecommendation(recommendationId: string) {
    const { data } = await api.post<ApiResponse<ApplyAIRecommendationResponse>>(`/ai/recommendations/${recommendationId}/apply`);
    return data.data;
  },
};
