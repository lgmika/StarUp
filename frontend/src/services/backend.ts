import api from "@/lib/api";
import { getRefreshToken } from "@/lib/auth";
import type { ApiResponse } from "@/types/api";
import type {
  AuthResponse,
  AuthUserDto,
  ForgotPasswordRequest,
  ForgotPasswordResponse,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
  VerifyEmailRequest,
} from "@/types/auth";
import type { ProjectSummaryDto } from "@/types/project";

export interface ApiInfoDto {
  name: string;
  version: string;
}

export interface HealthCheckDto {
  status: string;
  duration: number;
  checks: Array<{
    name: string;
    status: string;
    error?: string;
  }>;
}

export const backendService = {
  async getApiInfo() {
    const { data } = await api.get<ApiResponse<ApiInfoDto>>("/");
    return data.data;
  },

  async getHealth() {
    const { data } = await api.get<ApiResponse<HealthCheckDto>>("/health");
    return data.data;
  },

  async listProjects(search?: string) {
    const { data } = await api.get<ApiResponse<ProjectSummaryDto[]>>("/projects", {
      params: search ? { search } : undefined,
    });
    return data.data;
  },

  async login(request: LoginRequest) {
    const { data } = await api.post<ApiResponse<AuthResponse>>("/auth/login", request);
    return data.data;
  },

  async register(request: RegisterRequest) {
    const { data } = await api.post<ApiResponse<AuthResponse>>("/auth/register", request);
    return data.data;
  },

  async forgotPassword(request: ForgotPasswordRequest) {
    const { data } = await api.post<ApiResponse<ForgotPasswordResponse>>("/auth/forgot-password", request);
    return data.data;
  },

  async resetPassword(request: ResetPasswordRequest) {
    await api.post<ApiResponse<null>>("/auth/reset-password", request);
  },

  async verifyEmail(request: VerifyEmailRequest) {
    await api.post<ApiResponse<null>>("/auth/verify-email", request);
  },

  async getCurrentUser() {
    const { data } = await api.get<ApiResponse<AuthUserDto>>("/auth/me");
    return data.data;
  },

  async logout() {
    const refreshToken = getRefreshToken();
    if (!refreshToken) return;
    await api.post<ApiResponse<null>>("/auth/logout", { refreshToken });
  },
};
