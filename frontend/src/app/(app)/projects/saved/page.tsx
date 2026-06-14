"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { Bookmark, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { StatusBadge } from "@/components/workspace/status-badge";
import { getApiErrorMessage } from "@/lib/api";
import { PROJECT_STAGE_LABELS, PROJECT_STATUS_LABELS, PROJECT_VISIBILITY_LABELS } from "@/lib/constants";
import { projectService } from "@/services";
import type { ProjectSummaryDto } from "@/types/project";

export default function SavedProjectsPage() {
  const [projects, setProjects] = useState<ProjectSummaryDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [mutatingProjectId, setMutatingProjectId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function loadSavedProjects() {
    try {
      setError(null);
      setIsLoading(true);
      setProjects(await projectService.listSavedProjects());
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
    } finally {
      setIsLoading(false);
    }
  }

  async function unsaveProject(projectId: string) {
    try {
      setMutatingProjectId(projectId);
      await projectService.unsaveProject(projectId);
      setProjects((items) => items.filter((project) => project.id !== projectId));
      toast.success("Project removed from saved list.");
    } catch (mutationError) {
      toast.error(getApiErrorMessage(mutationError));
    } finally {
      setMutatingProjectId(null);
    }
  }

  useEffect(() => {
    void loadSavedProjects();
  }, []);

  if (isLoading) return <LoadingState label="Loading saved projects" />;

  return (
    <div className="space-y-5">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-end">
        <div>
          <h1 className="text-2xl font-semibold">Saved Projects</h1>
          <p className="mt-2 text-sm text-muted-foreground">Projects you saved for later review, powered by /users/me/saved-projects.</p>
        </div>
        <Link
          href="/projects"
          className="inline-flex h-10 items-center justify-center gap-2 rounded-md border border-border bg-background px-4 text-sm font-medium transition-colors hover:bg-accent"
        >
          <Search className="h-4 w-4" />
          Discover more
        </Link>
      </div>

      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}

      {projects.length === 0 ? (
        <EmptyState icon={Bookmark} title="No saved projects" description="Save projects from discovery to build a shortlist for later." />
      ) : (
        <Panel>
          <PanelHeader>
            <PanelTitle>Shortlist</PanelTitle>
          </PanelHeader>
          <PanelBody className="grid gap-3">
            {projects.map((project) => (
              <div key={project.id} className="rounded-md border border-border p-4">
                <div className="flex flex-col justify-between gap-3 lg:flex-row lg:items-start">
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <Link href={`/projects/${project.id}`} className="font-semibold hover:text-primary">{project.title}</Link>
                      <StatusBadge value={PROJECT_STATUS_LABELS[project.status]} />
                      <Badge tone="muted">{PROJECT_VISIBILITY_LABELS[project.visibility]}</Badge>
                    </div>
                    <p className="mt-2 line-clamp-2 text-sm text-muted-foreground">{project.summary}</p>
                    <div className="mt-3 flex flex-wrap gap-2 text-xs text-muted-foreground">
                      <span>{PROJECT_STAGE_LABELS[project.stage]}</span>
                      <span>Created {new Date(project.createdAt).toLocaleDateString()}</span>
                      <span>{project.isRecruiting ? "Recruiting" : "Not recruiting"}</span>
                    </div>
                  </div>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => void unsaveProject(project.id)}
                    disabled={mutatingProjectId === project.id}
                  >
                    <Trash2 className="h-4 w-4" />
                    Remove
                  </Button>
                </div>
              </div>
            ))}
          </PanelBody>
        </Panel>
      )}
    </div>
  );
}
