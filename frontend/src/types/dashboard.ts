export interface CountByStatusDto {
  status: string;
  count: number;
}

export interface UserDashboardDto {
  from: string;
  to: string;
  timezoneOffsetMinutes: number;
  applications: number;
  applicationsByStatus: CountByStatusDto[];
  upcomingInterviews: number;
  joinedProjects: number;
  savedProjects: number;
  profileCompletionPercent: number;
}

export interface ProjectStatusHistoryDto {
  id: string;
  fromStatus: string;
  toStatus: string;
  changedByUserId: string;
  reason?: string;
  createdAt: string;
}

export interface FounderProjectDashboardDto {
  projectId: string;
  projectTitle: string;
  from: string;
  to: string;
  timezoneOffsetMinutes: number;
  projectViews: number;
  savedCount: number;
  applications: number;
  applicationConversionRate: number;
  teamSize: number;
  investorInterests: number;
  ndaAgreements: number;
  applicationsByStatus: CountByStatusDto[];
  investorInterestsByStatus: CountByStatusDto[];
  projectStatusHistory: ProjectStatusHistoryDto[];
}
