import { backgroundJobService } from "@/services/background-job-service";

export const backgroundJobsApi = {
  list: backgroundJobService.list,
  run: backgroundJobService.run,
};
