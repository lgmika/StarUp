import api from "@/lib/api";
import type { ActivityListResponse } from "@/types/activity";
import type { ApiResponse } from "@/types/api";

export const activityService = {
  async getFeed(page = 1, pageSize = 20) {
    const { data } = await api.get<ApiResponse<ActivityListResponse>>("/feed", {
      params: { page, pageSize },
    });
    return data.data;
  },
};
