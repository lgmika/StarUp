import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type {
  CreateNdaTemplateRequest,
  CreateNdaTemplateVersionRequest,
  NdaTemplateDto,
  NdaTemplateVersionDto,
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
};
