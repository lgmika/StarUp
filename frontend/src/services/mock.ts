import { NotificationType, ReportStatus } from "@/types/enums";
import type { AdminDashboardDto, AdminUserDto, AuditLogDto } from "@/types/admin";
import type { NotificationDto } from "@/types/notification";
import type { ReportDto } from "@/types/report";

const now = new Date().toISOString();

const notifications: NotificationDto[] = [
  {
    id: "mock-notification-1",
    userId: "mock-user",
    type: NotificationType.System,
    title: "Backend endpoints pending",
    message: "Notifications are currently served by the frontend mock service.",
    readAt: undefined,
    isDeleted: false,
    createdAt: now,
  },
  {
    id: "mock-notification-2",
    userId: "mock-user",
    type: NotificationType.ProjectModeration,
    title: "Moderation flow ready",
    message: "Project review endpoints exist in the backend and can replace this mock later.",
    resourceType: "Project",
    isDeleted: false,
    createdAt: now,
  },
];

const reports: ReportDto[] = [
  {
    id: "mock-report-1",
    reporterUserId: "mock-user",
    targetType: "Project",
    targetId: "mock-project",
    reason: "Sample report until user report endpoints are implemented.",
    status: ReportStatus.Pending,
    createdAt: now,
  },
];

const users: AdminUserDto[] = [
  {
    id: "mock-admin-user-1",
    email: "founder@example.com",
    fullName: "Founder Example",
    isEmailVerified: true,
    isSuspended: false,
    roles: ["User", "VerifiedUser", "Business"],
    createdAt: now,
  },
  {
    id: "mock-admin-user-2",
    email: "investor@example.com",
    fullName: "Investor Example",
    isEmailVerified: true,
    isSuspended: false,
    roles: ["User", "VerifiedUser", "Investor"],
    createdAt: now,
  },
];

const auditLogs: AuditLogDto[] = [
  {
    id: "mock-audit-1",
    actorUserId: "mock-admin",
    action: "Mock.Admin.View",
    resourceType: "AdminDashboard",
    reason: "Frontend mock until admin endpoints are implemented.",
    createdAt: now,
  },
];

export const mockService = {
  async getNotifications() {
    return notifications.filter((notification) => !notification.isDeleted);
  },

  async getUnreadCount() {
    return notifications.filter((notification) => !notification.readAt && !notification.isDeleted).length;
  },

  async markNotificationRead(notificationId: string) {
    const notification = notifications.find((item) => item.id === notificationId);
    if (notification && !notification.readAt) {
      notification.readAt = new Date().toISOString();
    }
    return notification;
  },

  async markAllNotificationsRead() {
    const readAt = new Date().toISOString();
    notifications.forEach((notification) => {
      if (!notification.isDeleted && !notification.readAt) {
        notification.readAt = readAt;
      }
    });
    return notifications.filter((notification) => !notification.isDeleted);
  },

  async deleteNotification(notificationId: string) {
    const notification = notifications.find((item) => item.id === notificationId);
    if (notification) {
      notification.isDeleted = true;
    }
    return notification;
  },

  async getReports() {
    return reports;
  },

  async getAdminDashboard(): Promise<AdminDashboardDto> {
    return {
      totalUsers: users.length,
      totalProjects: 0,
      pendingProjects: 0,
      publishedProjects: 0,
      openReports: reports.filter((report) => report.status === ReportStatus.Pending).length,
      totalInvestorInterests: 0,
      totalApplications: 0,
    };
  },

  async getAdminUsers() {
    return users;
  },

  async getAuditLogs() {
    return auditLogs;
  },
};
