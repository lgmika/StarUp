import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type {
  AddUserSkillRequest,
  CreateCvRequest,
  CreatePortfolioRequest,
  CvDto,
  PortfolioDto,
  ProfileDto,
  SkillDto,
  UpdateCvRequest,
  UpsertProfileRequest,
} from "@/types/user";

export const profileService = {
  async getMyProfile() {
    const { data } = await api.get<ApiResponse<ProfileDto>>("/profiles/me");
    return data.data;
  },

  async createProfile(request: UpsertProfileRequest) {
    const { data } = await api.post<ApiResponse<ProfileDto>>("/profiles/me", request);
    return data.data;
  },

  async updateProfile(request: UpsertProfileRequest) {
    const { data } = await api.put<ApiResponse<ProfileDto>>("/profiles/me", request);
    return data.data;
  },

  async getPublicProfile(userId: string) {
    const { data } = await api.get<ApiResponse<ProfileDto>>(`/profiles/${userId}`);
    return data.data;
  },

  async listSkills() {
    const { data } = await api.get<ApiResponse<SkillDto[]>>("/skills");
    return data.data;
  },

  async addSkill(request: AddUserSkillRequest) {
    const { data } = await api.post<ApiResponse<SkillDto>>("/users/me/skills", request);
    return data.data;
  },

  async removeSkill(skillId: string) {
    await api.delete<ApiResponse<null>>(`/users/me/skills/${skillId}`);
  },

  async listCvs() {
    const { data } = await api.get<ApiResponse<CvDto[]>>("/cvs/me");
    return data.data;
  },

  async createCv(request: CreateCvRequest) {
    const { data } = await api.post<ApiResponse<CvDto>>("/cvs", request);
    return data.data;
  },

  async updateCv(cvId: string, request: UpdateCvRequest) {
    const { data } = await api.put<ApiResponse<CvDto>>(`/cvs/${cvId}`, request);
    return data.data;
  },

  async deleteCv(cvId: string) {
    await api.delete<ApiResponse<null>>(`/cvs/${cvId}`);
  },

  async uploadCv(file: File, onProgress?: (percent: number) => void) {
    const formData = new FormData();
    formData.append("file", file);

    const { data } = await api.post<ApiResponse<CvDto>>("/cvs/upload", formData, {
      headers: { "Content-Type": "multipart/form-data" },
      onUploadProgress: (event) => {
        if (!event.total) return;
        onProgress?.(Math.round((event.loaded / event.total) * 100));
      },
    });
    return data.data;
  },

  async createPortfolio(request: CreatePortfolioRequest) {
    const { data } = await api.post<ApiResponse<PortfolioDto>>("/portfolios", request);
    return data.data;
  },
};
