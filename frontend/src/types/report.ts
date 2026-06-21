import { ReportReasonCode, ReportStatus } from './enums';

export type { ReportReasonCode, ReportStatus };

export interface ReportDto {
  id: string;
  reporterUserId: string;
  reporterEmail?: string;
  targetType: string;
  targetId: string;
  reason?: string;
  reasonCode?: ReportReasonCode;
  description?: string;
  evidence?: string;
  status: ReportStatus;
  assignedModeratorId?: string;
  resolution?: string;
  createdAt: string;
  resolvedAt?: string;
}

export interface CreateReportRequest {
  targetType: string;
  targetId: string;
  reasonCode: ReportReasonCode;
  description: string;
  evidence?: string;
}

export interface ReportTargetContextDto {
  targetType: string;
  targetId: string;
  exists: boolean;
  canReport: boolean;
  displayName?: string | null;
  ownerEmail?: string | null;
  reason?: string | null;
}

export interface ReportActionDto {
  id: string;
  reportId: string;
  actorUserId: string;
  action: string;
  reason: string;
  createdAt: string;
}

export interface ReportDetailDto {
  report: ReportDto;
  actions: ReportActionDto[];
}

export interface ReportListResponse {
  items: ReportDto[];
  total: number;
  page: number;
  pageSize: number;
}
