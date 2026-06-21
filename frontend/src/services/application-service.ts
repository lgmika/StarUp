import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type {
  ApplicationDecisionRequest,
  ApplicationDetailDto,
  ApplicationDto,
  ApplyProjectRequest,
} from "@/types/application";

export const applicationService = {
  async apply(projectId: string, request: ApplyProjectRequest) {
    const { data } = await api.post<ApiResponse<ApplicationDto>>(`/projects/${projectId}/applications`, request);
    return data.data;
  },

  async listMyApplications() {
    const { data } = await api.get<ApiResponse<ApplicationDto[]>>("/users/me/applications");
    return data.data;
  },

  async listProjectApplications(projectId: string) {
    const { data } = await api.get<ApiResponse<ApplicationDto[]>>(`/projects/${projectId}/applications`);
    return data.data;
  },

  async getApplication(projectId: string, applicationId: string) {
    const { data } = await api.get<ApiResponse<ApplicationDetailDto>>(
      `/projects/${projectId}/applications/${applicationId}`,
      { skipForbiddenRedirect: true }
    );
    return data.data;
  },

  async withdraw(projectId: string, applicationId: string, request: ApplicationDecisionRequest) {
    await api.post<ApiResponse<null>>(`/projects/${projectId}/applications/${applicationId}/withdraw`, request);
  },

  async decide(projectId: string, applicationId: string, action: "shortlist" | "interview" | "accept" | "reject", request: ApplicationDecisionRequest) {
    const { data } = await api.post<ApiResponse<ApplicationDto>>(`/projects/${projectId}/applications/${applicationId}/${action}`, request);
    return data.data;
  },
};
