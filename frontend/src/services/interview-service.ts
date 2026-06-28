import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type { CreateInterviewRequest, InterviewDecisionRequest, InterviewDto, UpdateInterviewRequest } from "@/types/interview";

export const interviewService = {
  async listByApplication(applicationId: string) {
    const { data } = await api.get<ApiResponse<InterviewDto[]>>(`/applications/${applicationId}/interviews`);
    return data.data;
  },

  async schedule(applicationId: string, request: CreateInterviewRequest) {
    const { data } = await api.post<ApiResponse<InterviewDto>>(`/applications/${applicationId}/interviews`, request);
    return data.data;
  },

  async listMine() {
    const { data } = await api.get<ApiResponse<InterviewDto[]>>("/users/me/interviews");
    return data.data;
  },

  async update(interviewId: string, request: UpdateInterviewRequest) {
    const { data } = await api.put<ApiResponse<InterviewDto>>(`/interviews/${interviewId}`, request);
    return data.data;
  },

  async cancel(interviewId: string, request: InterviewDecisionRequest) {
    const { data } = await api.post<ApiResponse<InterviewDto>>(`/interviews/${interviewId}/cancel`, request);
    return data.data;
  },

  async complete(interviewId: string, request: InterviewDecisionRequest) {
    const { data } = await api.post<ApiResponse<InterviewDto>>(`/interviews/${interviewId}/complete`, request);
    return data.data;
  },
};
