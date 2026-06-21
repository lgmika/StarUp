import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type { FileDownloadUrlResponse, FileListResponse } from "@/types/file";

export const fileService = {
  async listMine(page = 1, pageSize = 20) {
    const { data } = await api.get<ApiResponse<FileListResponse>>("/files/me", { params: { page, pageSize } });
    return data.data;
  },

  async getDownloadUrl(fileId: string) {
    const { data } = await api.get<ApiResponse<FileDownloadUrlResponse>>(`/files/${fileId}/download-url`);
    return data.data;
  },

  async delete(fileId: string) {
    await api.delete<ApiResponse<null>>(`/files/${fileId}`);
  },
};
