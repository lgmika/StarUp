import { NotificationType } from './enums';

export interface NotificationDto {
  id: string;
  userId?: string;
  type: NotificationType;
  title: string;
  message: string;
  resourceId?: string;
  resourceType?: string;
  actionUrl?: string;
  isRead?: boolean;
  readAt?: string;
  isDeleted?: boolean;
  createdAt: string;
}

export interface UnreadCountDto {
  count?: number;
  unreadCount?: number;
}

export interface NotificationListResponse {
  items: NotificationDto[];
  total: number;
  unreadCount: number;
  page: number;
  pageSize: number;
}
