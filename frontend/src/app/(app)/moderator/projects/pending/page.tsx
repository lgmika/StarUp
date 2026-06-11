"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { ExternalLink, ListChecks, RefreshCw } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { RiskFlags } from "@/components/moderator/risk-flags";
import { ProjectStageBadge, ProjectStatusBadge } from "@/components/projects/project-badges";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { moderatorService } from "@/services";
import type { ModeratorProjectQueueItemDto } from "@/types/moderator";

export default function PendingModerationProjectsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Moderator, SystemRoles.Admin]}>
      <PendingQueue />
    </RoleGuard>
  );
}

function PendingQueue() {
  const [projects, setProjects] = useState<ModeratorProjectQueueItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function loadProjects() {
    setIsLoading(true);
    setError(null);
    try {
      setProjects(await moderatorService.listPendingProjects());
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadProjects();
  }, []);

  if (isLoading) return <LoadingState label="Loading pending projects" />;

  return (
    <div className="space-y-5">
      <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
        <div>
          <h1 className="text-2xl font-semibold">Pending Projects</h1>
          <p className="mt-2 text-sm text-muted-foreground">Projects submitted for moderator review.</p>
        </div>
        <Button variant="outline" onClick={() => void loadProjects()}>
          <RefreshCw className="h-4 w-4" />
          Refresh
        </Button>
      </div>
      {error ? <p className="text-sm text-destructive">{error}</p> : null}
      {projects.length === 0 ? (
        <Panel>
          <PanelBody className="py-12 text-center">
            <ListChecks className="mx-auto h-8 w-8 text-muted-foreground" />
            <h2 className="mt-4 text-base font-semibold">No pending projects</h2>
            <p className="mt-2 text-sm text-muted-foreground">The moderation queue is clear.</p>
          </PanelBody>
        </Panel>
      ) : null}
      <div className="grid gap-3">
        {projects.map((project) => (
          <Panel key={project.projectId}>
            <PanelBody className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <h2 className="text-base font-semibold">{project.title}</h2>
                  <ProjectStatusBadge status={project.status} />
                  <ProjectStageBadge stage={project.stage} />
                </div>
                <p className="mt-2 line-clamp-2 text-sm leading-6 text-muted-foreground">{project.summary}</p>
                <div className="mt-3">
                  <RiskFlags flags={project.latestAIRiskFlags} />
                </div>
                <p className="mt-2 text-xs text-muted-foreground">
                  AI score: {project.latestAIQualityScore ?? "not reviewed"} · Submitted {project.submittedAt ? new Date(project.submittedAt).toLocaleString() : "unknown"}
                </p>
              </div>
              <Link className="inline-flex h-9 items-center justify-center gap-2 rounded-md border border-border px-3 text-sm font-medium hover:bg-accent" href={`/moderator/projects/${project.projectId}`}>
                <ExternalLink className="h-4 w-4" />
                Review
              </Link>
            </PanelBody>
          </Panel>
        ))}
      </div>
    </div>
  );
}
