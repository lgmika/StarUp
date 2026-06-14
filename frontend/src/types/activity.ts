export interface ActivityDto {
  id: string;
  projectId: string;
  projectTitle: string;
  actorUserId?: string;
  actorName?: string;
  type: string;
  visibility: string;
  title: string;
  message?: string;
  targetType?: string;
  targetId?: string;
  createdAt: string;
}

export interface ActivityListResponse {
  items: ActivityDto[];
  total: number;
  page: number;
  pageSize: number;
}
