import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type { UserDashboardDto } from "@/types/dashboard";

interface DashboardQuery {
  from?: string;
  to?: string;
  timezoneOffsetMinutes?: number;
}

export const dashboardService = {
  async getMine(params: DashboardQuery = {}) {
    const { data } = await api.get<ApiResponse<UserDashboardDto>>("/dashboard/me", { params });
    return data.data;
  },
};
