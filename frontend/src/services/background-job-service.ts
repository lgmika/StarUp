import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type { BackgroundJobExecutionDto, BackgroundJobRunResult } from "@/types/background-job";

export const backgroundJobService = {
  async list(limit = 50) {
    const { data } = await api.get<ApiResponse<BackgroundJobExecutionDto[]>>("/admin/background-jobs", {
      params: { limit },
    });
    return data.data;
  },

  async run() {
    const { data } = await api.post<ApiResponse<BackgroundJobRunResult>>("/admin/background-jobs/run");
    return data.data;
  },
};
