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
