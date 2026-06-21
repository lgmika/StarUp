import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type {
  CreateNdaTemplateRequest,
  CreateNdaTemplateVersionRequest,
  NdaTemplateDto,
  NdaTemplateVersionDto,
  CurrentProjectNdaDto,
  NdaAgreementDto,
} from "@/types/nda";

export const ndaService = {
  async listTemplates() {
    const { data } = await api.get<ApiResponse<NdaTemplateDto[]>>("/nda/templates");
    return data.data;
  },

  async createTemplate(request: CreateNdaTemplateRequest) {
    const { data } = await api.post<ApiResponse<NdaTemplateDto>>("/nda/templates", request);
    return data.data;
  },

  async createTemplateVersion(templateId: string, request: CreateNdaTemplateVersionRequest) {
    const { data } = await api.post<ApiResponse<NdaTemplateVersionDto>>(`/nda/templates/${templateId}/versions`, request);
    return data.data;
  },

  async listMyAgreements() {
    const { data } = await api.get<ApiResponse<NdaAgreementDto[]>>("/users/me/nda-agreements");
    return data.data;
  },

  async getCurrentProjectNda(projectId: string) {
    const { data } = await api.get<ApiResponse<CurrentProjectNdaDto>>(`/projects/${projectId}/nda/current`);
    return data.data;
  },

  async acceptProjectNda(projectId: string) {
    const { data } = await api.post<ApiResponse<NdaAgreementDto>>(`/projects/${projectId}/nda/accept`);
    return data.data;
  },

  async listProjectAgreements(projectId: string) {
    const { data } = await api.get<ApiResponse<NdaAgreementDto[]>>(`/projects/${projectId}/nda/agreements`);
    return data.data;
  },
};
