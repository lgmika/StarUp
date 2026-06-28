import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type { AITextResponse } from "@/types/ai";
import type {
  CreateInvestorInterestRequest,
  InvestorInterestDto,
  InvestorDashboardDto,
  InvestorProjectDiscoveryDto,
  InvestorProfileDto,
  UpsertInvestorProfileRequest,
} from "@/types/investor";

export const investorService = {
  async getDashboard() {
    const { data } = await api.get<ApiResponse<InvestorDashboardDto>>("/investors/me/dashboard", { params: { timezoneOffsetMinutes: new Date().getTimezoneOffset() } });
    return data.data;
  },
  async getMyProfile() {
    const { data } = await api.get<ApiResponse<InvestorProfileDto>>("/investors/me/profile", {
      skipForbiddenRedirect: true,
    });
    return data.data;
  },

  async createProfile(request: UpsertInvestorProfileRequest) {
    const { data } = await api.post<ApiResponse<InvestorProfileDto>>("/investors/me/profile", request);
    return data.data;
  },

  async updateProfile(request: UpsertInvestorProfileRequest) {
    const { data } = await api.put<ApiResponse<InvestorProfileDto>>("/investors/me/profile", request);
    return data.data;
  },

  async listProjects(search?: string) {
    const keyword = search?.trim();
    const { data } = await api.get<ApiResponse<InvestorProjectDiscoveryDto[]>>("/investors/projects", {
      params: keyword ? { search: keyword } : undefined,
    });
    return data.data;
  },

  async createInvestorSummary(projectId: string) {
    const { data } = await api.post<ApiResponse<AITextResponse>>(`/projects/${projectId}/ai/investor-summary`);
    return data.data;
  },

  async expressInterest(projectId: string, request: CreateInvestorInterestRequest) {
    const { data } = await api.post<ApiResponse<InvestorInterestDto>>(`/projects/${projectId}/investor-interests`, request);
    return data.data;
  },

  async listMyInterests() {
    const { data } = await api.get<ApiResponse<InvestorInterestDto[]>>("/investors/me/interests");
    return data.data;
  },

  async listProjectInterests(projectId: string) {
    const { data } = await api.get<ApiResponse<InvestorInterestDto[]>>(`/projects/${projectId}/investor-interests`);
    return data.data;
  },

  async decideInterest(projectId: string, interestId: string, action: "accept" | "reject" | "request-more-info", response?: string) {
    const { data } = await api.post<ApiResponse<InvestorInterestDto>>(
      `/projects/${projectId}/investor-interests/${interestId}/${action}`,
      { response: response?.trim() || undefined }
    );
    return data.data;
  },

  async withdrawInterest(projectId: string, interestId: string) {
    const { data } = await api.post<ApiResponse<InvestorInterestDto>>(`/projects/${projectId}/investor-interests/${interestId}/withdraw`);
    return data.data;
  },
};
