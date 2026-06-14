import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type {
  MemberRecommendationDto,
  ProjectRecommendationDto,
  RecommendationListResponse,
} from "@/types/recommendation";

export const recommendationService = {
  async projects(page = 1, pageSize = 20) {
    const { data } = await api.get<ApiResponse<RecommendationListResponse<ProjectRecommendationDto>>>("/recommendations/projects", {
      params: { page, pageSize },
    });
    return data.data;
  },

  async members(page = 1, pageSize = 20) {
    const { data } = await api.get<ApiResponse<RecommendationListResponse<MemberRecommendationDto>>>("/recommendations/members", {
      params: { page, pageSize },
    });
    return data.data;
  },

  async dismiss(recommendationId: string) {
    await api.post<ApiResponse<null>>(`/recommendations/${recommendationId}/dismiss`);
  },
};
