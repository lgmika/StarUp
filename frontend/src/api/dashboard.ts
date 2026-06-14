import { activityService } from "@/services/activity-service";
import { dashboardService } from "@/services/dashboard-service";

export const dashboardApi = {
  getMine: dashboardService.getMine,
  getFeed: activityService.getFeed,
};
