import { InvestorInterestStatus } from './enums';
import { ProjectSummaryDto } from './project';

export interface InvestorProfileDto {
  id: string;
  userId: string;
  displayName: string;
  organizationName?: string;
  bio?: string;
  investmentFocus?: string;
  websiteUrl?: string;
  linkedInUrl?: string;
  minTicketSize?: number;
  maxTicketSize?: number;
}

export interface UpsertInvestorProfileRequest {
  displayName: string;
  organizationName?: string;
  bio?: string;
  investmentFocus?: string;
  websiteUrl?: string;
  linkedInUrl?: string;
  minTicketSize?: number;
  maxTicketSize?: number;
}

export interface CreateInvestorInterestRequest {
  message: string;
}

export interface InvestorInterestDecisionRequest {
  response?: string;
}

export interface InvestorInterestDto {
  id: string;
  projectId: string;
  projectTitle: string;
  investorUserId: string;
  investorEmail: string;
  message: string;
  status: InvestorInterestStatus;
  founderResponse?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface InvestorProjectDiscoveryDto {
  project: ProjectSummaryDto;
  investorSummary?: string;
}

export interface InvestorDashboardDto {
  from: string;
  to: string;
  timezoneOffsetMinutes: number;
  interestedProjects: number;
  interestStatus: Array<{ status: string; count: number }>;
  ndaPending: number;
  acceptedAccess: number;
  savedProjects: number;
}
