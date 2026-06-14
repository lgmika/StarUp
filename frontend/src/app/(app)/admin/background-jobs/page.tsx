"use client";

import { useEffect, useState } from "react";
import { Play } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { PageHeader } from "@/components/workspace/page-header";
import { StatusBadge } from "@/components/workspace/status-badge";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { backgroundJobService } from "@/services/background-job-service";
import type { BackgroundJobExecutionDto } from "@/types/background-job";

export default function BackgroundJobsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <BackgroundJobs />
    </RoleGuard>
  );
}

function BackgroundJobs() {
  const [jobs, setJobs] = useState<BackgroundJobExecutionDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRunning, setIsRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadJobs() {
      try {
        setJobs(await backgroundJobService.list());
      } catch (loadError) {
        setError(getApiErrorMessage(loadError));
      } finally {
        setIsLoading(false);
      }
    }

    void loadJobs();
  }, []);

  async function runJob() {
    setIsRunning(true);
    setError(null);
    try {
      const result = await backgroundJobService.run();
      setJobs(result.executions);
    } catch (runError) {
      setError(getApiErrorMessage(runError));
    } finally {
      setIsRunning(false);
    }
  }

  if (isLoading) return <LoadingState label="Loading background jobs" />;

  return (
    <div className="space-y-5">
      <PageHeader
        title="Background Jobs"
        description="Monitor job executions and run backend maintenance through POST /admin/background-jobs/run."
        actions={
          <Button onClick={() => void runJob()} disabled={isRunning}>
            <Play className="h-4 w-4" />
            {isRunning ? "Running" : "Run job"}
          </Button>
        }
      />
      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}
      <Panel>
        <PanelHeader><PanelTitle>Executions</PanelTitle></PanelHeader>
        <PanelBody className="overflow-x-auto">
          <table className="w-full min-w-[900px] text-left text-sm">
            <thead className="border-b border-border text-xs text-muted-foreground">
              <tr>
                <th className="py-2 font-medium">Job name</th>
                <th className="py-2 font-medium">Status</th>
                <th className="py-2 font-medium">Started at</th>
                <th className="py-2 font-medium">Finished at</th>
                <th className="py-2 font-medium">Attempt</th>
                <th className="py-2 font-medium">Items processed</th>
                <th className="py-2 font-medium">Error</th>
              </tr>
            </thead>
            <tbody>
              {jobs.map((job) => (
                <tr key={job.id} className="border-b border-border">
                  <td className="py-3 font-medium">{job.jobName}</td>
                  <td className="py-3"><StatusBadge value={job.status} /></td>
                  <td className="py-3 text-muted-foreground">{job.startedAt}</td>
                  <td className="py-3 text-muted-foreground">{job.finishedAt}</td>
                  <td className="py-3">{job.attempt}</td>
                  <td className="py-3">{job.itemsProcessed}</td>
                  <td className="py-3 text-muted-foreground">{job.error ?? "-"}</td>
                </tr>
              ))}
              {jobs.length === 0 ? (
                <tr>
                  <td className="py-8 text-center text-sm text-muted-foreground" colSpan={7}>No background job executions yet.</td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </PanelBody>
      </Panel>
    </div>
  );
}
