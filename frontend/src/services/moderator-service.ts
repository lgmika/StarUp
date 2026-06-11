import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type {
  ModerationDecisionRequest,
  ModeratorDashboardDto,
  ModeratorProjectDetailDto,
  ModeratorProjectQueueItemDto,
} from "@/types/moderator";

export const moderatorService = {
  async getDashboard() {
    const { data } = await api.get<ApiResponse<ModeratorDashboardDto>>("/moderator/dashboard");
    return data.data;
  },

  async listPendingProjects() {
    const { data } = await api.get<ApiResponse<ModeratorProjectQueueItemDto[]>>("/moderator/projects/pending");
    return data.data;
  },

  async getProject(projectId: string) {
    const { data } = await api.get<ApiResponse<ModeratorProjectDetailDto>>(`/moderator/projects/${projectId}`, {
      skipForbiddenRedirect: true,
    });
    return data.data;
  },

  async approve(projectId: string, request: ModerationDecisionRequest) {
    await api.post<ApiResponse<null>>(`/moderator/projects/${projectId}/approve`, request);
  },

  async requestImprovement(projectId: string, request: ModerationDecisionRequest) {
    await api.post<ApiResponse<null>>(`/moderator/projects/${projectId}/request-improvement`, request);
  },

  async reject(projectId: string, request: ModerationDecisionRequest) {
    await api.post<ApiResponse<null>>(`/moderator/projects/${projectId}/reject`, request);
  },

  async hide(projectId: string, request: ModerationDecisionRequest) {
    await api.post<ApiResponse<null>>(`/moderator/projects/${projectId}/hide`, request);
  },

  async restore(projectId: string, request: ModerationDecisionRequest) {
    await api.post<ApiResponse<null>>(`/moderator/projects/${projectId}/restore`, request);
  },
};
