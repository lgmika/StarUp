import { ReportStatus } from './enums';

// Note: Backend report endpoints for users are not yet implemented.
// These types are prepared for future integration.

export interface ReportDto {
  id: string;
  reporterUserId: string;
  targetType: string;
  targetId: string;
  reason: string;
  status: ReportStatus;
  createdAt: string;
}

export interface CreateReportRequest {
  targetType: string;
  targetId: string;
  reason: string;
}
