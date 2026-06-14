import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type {
  InvestorSearchItemDto,
  InvestorSearchParams,
  MemberSearchItemDto,
  MemberSearchParams,
  ProjectSearchItemDto,
  ProjectSearchParams,
  SearchResultPage,
  SearchSuggestionsResponse,
} from "@/types/search";

export const searchService = {
  async projects(params: ProjectSearchParams) {
    const { data } = await api.get<ApiResponse<SearchResultPage<ProjectSearchItemDto>>>("/search/projects", { params });
    return data.data;
  },

  async members(params: MemberSearchParams) {
    const { data } = await api.get<ApiResponse<SearchResultPage<MemberSearchItemDto>>>("/search/members", { params });
    return data.data;
  },

  async investors(params: InvestorSearchParams) {
    const { data } = await api.get<ApiResponse<SearchResultPage<InvestorSearchItemDto>>>("/search/investors", { params });
    return data.data;
  },

  async suggestions(keyword?: string, limit = 10) {
    const { data } = await api.get<ApiResponse<SearchSuggestionsResponse>>("/search/suggestions", {
      params: { keyword, limit },
    });
    return data.data;
  },
};
