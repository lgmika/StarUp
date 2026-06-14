import { searchService } from "@/services/search-service";

export const searchApi = {
  projects: searchService.projects,
  members: searchService.members,
  investors: searchService.investors,
  suggestions: searchService.suggestions,
};
