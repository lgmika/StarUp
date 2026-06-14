import { recommendationService } from "@/services/recommendation-service";

export const recommendationsApi = {
  projects: recommendationService.projects,
  members: recommendationService.members,
  dismiss: recommendationService.dismiss,
};
