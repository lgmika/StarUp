import { mockService } from "./mock";

export const notificationService = {
  async listNotifications() {
    return mockService.getNotifications();
  },

  async getUnreadCount() {
    return mockService.getUnreadCount();
  },

  async markRead(notificationId: string) {
    return mockService.markNotificationRead(notificationId);
  },

  async markAllRead() {
    return mockService.markAllNotificationsRead();
  },

  async delete(notificationId: string) {
    return mockService.deleteNotification(notificationId);
  },
};
