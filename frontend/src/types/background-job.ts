export interface BackgroundJobExecutionDto {
  id: string;
  jobName: string;
  status: "Succeeded" | "Failed" | "Skipped";
  startedAt: string;
  finishedAt: string;
  attempt: number;
  itemsProcessed: number;
  lockKey?: number;
  error?: string;
}

export interface BackgroundJobRunResult {
  lockAcquired: boolean;
  executions: BackgroundJobExecutionDto[];
}
