import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type { NotificationDto, NotificationListResponse, UnreadCountDto } from "@/types/notification";

export const notificationService = {
  async list(params: { status?: string; type?: string; page?: number; pageSize?: number } = {}) {
    const { data } = await api.get<ApiResponse<NotificationListResponse>>("/notifications", { params });
    return data.data;
  },

  async listNotifications() {
    const { data } = await api.get<ApiResponse<NotificationListResponse>>("/notifications", { params: { page: 1, pageSize: 20 } });
    return data.data.items;
  },

  async getUnreadCount() {
    const { data } = await api.get<ApiResponse<UnreadCountDto>>("/notifications/unread-count");
    return data.data.unreadCount ?? data.data.count ?? 0;
  },

  async markRead(notificationId: string) {
    const { data } = await api.post<ApiResponse<NotificationDto>>(`/notifications/${notificationId}/read`);
    return data.data;
  },

  async markAllRead() {
    await api.post<ApiResponse<{ updatedCount: number }>>("/notifications/read-all");
    return this.listNotifications();
  },

  async delete(notificationId: string) {
    await api.delete<ApiResponse<null>>(`/notifications/${notificationId}`);
  },
};
