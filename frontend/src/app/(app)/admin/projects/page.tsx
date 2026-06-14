"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { FolderKanban, Search } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { StatusBadge } from "@/components/workspace/status-badge";
import { getApiErrorMessage } from "@/lib/api";
import { PROJECT_STAGE_LABELS, PROJECT_STATUS_LABELS, PROJECT_VISIBILITY_LABELS, SystemRoles } from "@/lib/constants";
import { projectService } from "@/services";
import type { ProjectSummaryDto } from "@/types/project";

export default function AdminProjectsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <AdminProjects />
    </RoleGuard>
  );
}

function AdminProjects() {
  const [projects, setProjects] = useState<ProjectSummaryDto[]>([]);
  const [query, setQuery] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const statusCounts = useMemo(() => {
    return projects.reduce<Record<string, number>>((acc, project) => {
      acc[project.status] = (acc[project.status] ?? 0) + 1;
      return acc;
    }, {});
  }, [projects]);

  async function loadProjects(search = query) {
    try {
      setError(null);
      setIsLoading(true);
      setProjects(await projectService.listProjects(search));
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
      setProjects([]);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadProjects("");
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="space-y-5">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-end">
        <div>
          <h1 className="text-2xl font-semibold">Admin Projects</h1>
          <p className="mt-2 text-sm text-muted-foreground">Review project inventory using the backend project discovery endpoint.</p>
        </div>
        <Link
          href="/moderator/projects/pending"
          className="inline-flex h-10 items-center justify-center rounded-md border border-border bg-background px-4 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground"
        >
          Moderation queue
        </Link>
      </div>

      <div className="grid gap-3 md:grid-cols-4">
        <Metric label="Total" value={projects.length} />
        <Metric label="Pending review" value={statusCounts.PendingReview ?? 0} />
        <Metric label="Published" value={statusCounts.Published ?? 0} />
        <Metric label="Hidden" value={statusCounts.Hidden ?? 0} />
      </div>

      <Panel>
        <PanelBody>
          <form
            className="flex flex-col gap-3 sm:flex-row"
            onSubmit={(event) => {
              event.preventDefault();
              void loadProjects();
            }}
          >
            <div className="relative flex-1">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input className="pl-9" placeholder="Search projects" value={query} onChange={(event) => setQuery(event.target.value)} />
            </div>
            <Button type="submit" disabled={isLoading}>
              <Search className="h-4 w-4" />
              Search
            </Button>
          </form>
        </PanelBody>
      </Panel>

      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}

      {isLoading ? (
        <LoadingState label="Loading projects" />
      ) : projects.length === 0 ? (
        <EmptyState icon={FolderKanban} title="No projects found" description="No projects were returned by the backend for the current search." />
      ) : (
        <Panel>
          <PanelHeader><PanelTitle>Projects</PanelTitle></PanelHeader>
          <PanelBody className="overflow-x-auto">
            <table className="w-full min-w-[900px] text-left text-sm">
              <thead className="border-b border-border text-xs text-muted-foreground">
                <tr>
                  <th className="py-2 font-medium">Project</th>
                  <th className="py-2 font-medium">Status</th>
                  <th className="py-2 font-medium">Stage</th>
                  <th className="py-2 font-medium">Visibility</th>
                  <th className="py-2 font-medium">Recruiting</th>
                  <th className="py-2 font-medium">Created</th>
                </tr>
              </thead>
              <tbody>
                {projects.map((project) => (
                  <tr key={project.id} className="border-b border-border">
                    <td className="py-3">
                      <Link href={`/projects/${project.id}`} className="font-medium hover:text-primary">{project.title}</Link>
                      <p className="mt-1 line-clamp-1 text-xs text-muted-foreground">{project.summary}</p>
                    </td>
                    <td className="py-3"><StatusBadge value={PROJECT_STATUS_LABELS[project.status]} /></td>
                    <td className="py-3">{PROJECT_STAGE_LABELS[project.stage]}</td>
                    <td className="py-3">{PROJECT_VISIBILITY_LABELS[project.visibility]}</td>
                    <td className="py-3"><Badge tone={project.isRecruiting ? "success" : "muted"}>{project.isRecruiting ? "Open" : "Closed"}</Badge></td>
                    <td className="py-3 text-muted-foreground">{new Date(project.createdAt).toLocaleDateString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </PanelBody>
        </Panel>
      )}
    </div>
  );
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <Panel>
      <PanelBody>
        <p className="text-2xl font-semibold">{value}</p>
        <p className="mt-1 text-sm text-muted-foreground">{label}</p>
      </PanelBody>
    </Panel>
  );
}
