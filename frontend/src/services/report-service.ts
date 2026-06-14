import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type { CreateReportRequest, ReportDetailDto, ReportListResponse, ReportStatus } from "@/types/report";

export interface ReportQueryParams {
  status?: ReportStatus;
  targetType?: string;
  reasonCode?: string;
  page?: number;
  pageSize?: number;
}

export const reportService = {
  async create(request: CreateReportRequest) {
    const { data } = await api.post<ApiResponse<ReportDetailDto["report"]>>("/reports", request);
    return data.data;
  },

  async listMyReports(params: ReportQueryParams = {}) {
    const { data } = await api.get<ApiResponse<ReportListResponse>>("/users/me/reports", { params });
    return data.data;
  },

  async getMyReport(reportId: string) {
    const { data } = await api.get<ApiResponse<ReportDetailDto>>(`/users/me/reports/${reportId}`);
    return data.data;
  },

  async listModeratorReports(params: ReportQueryParams = {}) {
    const { data } = await api.get<ApiResponse<ReportListResponse>>("/moderator/reports", { params });
    return data.data;
  },

  async assign(reportId: string, reason: string) {
    const { data } = await api.post<ApiResponse<ReportDetailDto>>(`/moderator/reports/${reportId}/assign`, { reason });
    return data.data;
  },

  async investigate(reportId: string, reason: string) {
    const { data } = await api.post<ApiResponse<ReportDetailDto>>(`/moderator/reports/${reportId}/investigate`, { reason });
    return data.data;
  },

  async resolve(reportId: string, resolution: string) {
    const { data } = await api.post<ApiResponse<ReportDetailDto>>(`/moderator/reports/${reportId}/resolve`, { resolution });
    return data.data;
  },

  async dismiss(reportId: string, reason: string) {
    const { data } = await api.post<ApiResponse<ReportDetailDto>>(`/moderator/reports/${reportId}/dismiss`, { reason });
    return data.data;
  },
};
