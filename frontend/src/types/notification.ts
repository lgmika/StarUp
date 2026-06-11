import { NotificationType } from './enums';

// Note: Backend notification endpoints are not yet implemented.
// These types are prepared for future integration.

export interface NotificationDto {
  id: string;
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  resourceId?: string;
  resourceType?: string;
  readAt?: string;
  isDeleted: boolean;
  createdAt: string;
}

export interface UnreadCountDto {
  count: number;
}
