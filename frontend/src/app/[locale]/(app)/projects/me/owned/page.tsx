"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { Archive, BarChart3, CheckCircle2, ExternalLink, FolderPlus, Loader2, Lock, Pencil, Power, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { AuthMessage } from "@/components/auth/auth-message";
import { LoadingState } from "@/components/common/loading-state";
import {
  ProjectStageBadge,
  ProjectStatusBadge,
  ProjectVisibilityBadge,
} from "@/components/projects/project-badges";
import {
  canArchiveProject,
  canCloseProject,
  canEditProject,
  canSubmitProject,
  getStatusActionHint,
} from "@/components/projects/project-actions";
import { ProjectVersionPanel } from "@/components/projects/project-version-panel";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { projectService } from "@/services";
import type { ProjectSummaryDto } from "@/types/project";

type LoadState = "loading" | "ready" | "error";
type ActionName = "submit" | "close" | "archive";

export default function OwnedProjectsPage() {
  const [projects, setProjects] = useState<ProjectSummaryDto[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState<string | null>(null);
  const [state, setState] = useState<LoadState>("loading");
  const [error, setError] = useState<string | null>(null);
  const [runningAction, setRunningAction] = useState<string | null>(null);

  const selectedProject = useMemo(
    () => projects.find((project) => project.id === selectedProjectId) ?? projects[0] ?? null,
    [projects, selectedProjectId]
  );

  async function loadOwnedProjects() {
    setState("loading");
    setError(null);
    try {
      const result = await projectService.listOwnedProjects();
      setProjects(result);
      setSelectedProjectId((current) => current ?? result[0]?.id ?? null);
      setState("ready");
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
      setState("error");
    }
  }

  async function runProjectAction(project: ProjectSummaryDto, action: ActionName) {
    const actionKey = `${action}:${project.id}`;
    setRunningAction(actionKey);
    try {
      if (action === "submit") {
        await projectService.submitReview(project.id);
        toast.success("Project submitted for review.");
      }
      if (action === "close") {
        await projectService.closeProject(project.id);
        toast.success("Project closed.");
      }
      if (action === "archive") {
        await projectService.archiveProject(project.id);
        toast.success("Project archived.");
      }
      await loadOwnedProjects();
    } catch (actionError) {
      toast.error(getApiErrorMessage(actionError));
    } finally {
      setRunningAction(null);
    }
  }

  useEffect(() => {
    void loadOwnedProjects();
  }, []);

  if (state === "loading") return <LoadingState label="Loading your projects" />;

  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_380px]">
      <section className="space-y-5">
        <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
          <div>
            <h1 className="text-2xl font-semibold">My Projects</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              Manage founder-owned projects returned by the backend. Actions are enabled according to project status.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={() => void loadOwnedProjects()}>
              <RefreshCw className="h-4 w-4" />
              Refresh
            </Button>
            <Link
              className="inline-flex h-10 items-center justify-center gap-2 rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90"
              href="/projects/create"
            >
              <FolderPlus className="h-4 w-4" />
              Create project
            </Link>
          </div>
        </div>

        {state === "error" && error ? <AuthMessage tone="error">{error}</AuthMessage> : null}

        {state === "ready" && projects.length === 0 ? (
          <Panel>
            <PanelBody className="py-12 text-center">
              <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-md bg-muted text-muted-foreground">
                <FolderPlus className="h-5 w-5" />
              </div>
              <h2 className="mt-4 text-base font-semibold">No owned projects yet</h2>
              <p className="mt-2 text-sm text-muted-foreground">Create your first draft to start building a team.</p>
              <Link
                className="mt-5 inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                href="/projects/create"
              >
                Create project
              </Link>
            </PanelBody>
          </Panel>
        ) : null}

        <div className="space-y-3">
          {projects.map((project) => (
            <ProjectManagementRow
              key={project.id}
              project={project}
              isSelected={selectedProject?.id === project.id}
              runningAction={runningAction}
              onSelect={() => setSelectedProjectId(project.id)}
              onAction={runProjectAction}
            />
          ))}
        </div>
      </section>

      <aside className="space-y-4">
        {selectedProject ? (
          <>
            <Panel>
              <PanelHeader>
                <PanelTitle>Action state</PanelTitle>
              </PanelHeader>
              <PanelBody className="space-y-3">
                <div className="flex flex-wrap gap-2">
                  <ProjectStatusBadge status={selectedProject.status} />
                  <ProjectStageBadge stage={selectedProject.stage} />
                  <ProjectVisibilityBadge visibility={selectedProject.visibility} />
                </div>
                <p className="text-sm leading-6 text-muted-foreground">{getStatusActionHint(selectedProject.status)}</p>
                <div className="grid gap-2 text-sm">
                  <Capability label="Edit" enabled={canEditProject(selectedProject.status)} />
                  <Capability label="Submit review" enabled={canSubmitProject(selectedProject.status)} />
                  <Capability label="Close" enabled={canCloseProject(selectedProject.status)} />
                  <Capability label="Archive" enabled={canArchiveProject(selectedProject.status)} />
                </div>
              </PanelBody>
            </Panel>
            <ProjectVersionPanel projectId={selectedProject.id} />
          </>
        ) : (
          <Panel>
            <PanelBody className="text-sm text-muted-foreground">Select a project to view action state and versions.</PanelBody>
          </Panel>
        )}
      </aside>
    </div>
  );
}

function ProjectManagementRow({
  project,
  isSelected,
  runningAction,
  onSelect,
  onAction,
}: {
  project: ProjectSummaryDto;
  isSelected: boolean;
  runningAction: string | null;
  onSelect: () => void;
  onAction: (project: ProjectSummaryDto, action: ActionName) => Promise<void>;
}) {
  return (
    <Panel className={isSelected ? "border-primary" : undefined}>
      <PanelBody className="space-y-4">
        <div className="flex flex-col justify-between gap-3 lg:flex-row lg:items-start">
          <button className="min-w-0 text-left" type="button" onClick={onSelect}>
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-base font-semibold">{project.title}</h2>
              {isSelected ? <Badge tone="default">Selected</Badge> : null}
            </div>
            <p className="mt-2 line-clamp-2 text-sm leading-6 text-muted-foreground">{project.summary}</p>
          </button>
          <div className="flex flex-wrap gap-2">
            <ProjectStatusBadge status={project.status} />
            <ProjectStageBadge stage={project.stage} />
            <ProjectVisibilityBadge visibility={project.visibility} />
            {project.isRecruiting ? <Badge tone="success">Recruiting</Badge> : <Badge tone="muted">Not recruiting</Badge>}
          </div>
        </div>

        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border pt-3">
          <p className="text-xs text-muted-foreground">
            Created {new Date(project.createdAt).toLocaleDateString()} · {project.slug}
          </p>
          <div className="flex flex-wrap gap-2">
            <Link
              className="inline-flex h-8 items-center justify-center gap-2 rounded-md border border-border bg-background px-3 text-xs font-medium hover:bg-accent"
              href={`/projects/${project.id}/dashboard`}
            >
              <BarChart3 className="h-3.5 w-3.5" />
              Manage
            </Link>
            <Link
              className="inline-flex h-8 items-center justify-center gap-2 rounded-md border border-border bg-background px-3 text-xs font-medium hover:bg-accent"
              href={`/projects/${project.id}`}
            >
              <ExternalLink className="h-3.5 w-3.5" />
              View
            </Link>
            <Link
              aria-disabled={!canEditProject(project.status)}
              className={
                canEditProject(project.status)
                  ? "inline-flex h-8 items-center justify-center gap-2 rounded-md border border-border bg-background px-3 text-xs font-medium hover:bg-accent"
                  : "inline-flex h-8 pointer-events-none items-center justify-center gap-2 rounded-md border border-border bg-muted px-3 text-xs font-medium text-muted-foreground opacity-60"
              }
              href={`/projects/${project.id}/edit`}
            >
              <Pencil className="h-3.5 w-3.5" />
              Edit
            </Link>
            <ActionButton
              actionKey={`submit:${project.id}`}
              disabled={!canSubmitProject(project.status)}
              icon={<CheckCircle2 className="h-3.5 w-3.5" />}
              label="Submit"
              runningAction={runningAction}
              onClick={() => onAction(project, "submit")}
            />
            <ActionButton
              actionKey={`close:${project.id}`}
              disabled={!canCloseProject(project.status)}
              icon={<Power className="h-3.5 w-3.5" />}
              label="Close"
              runningAction={runningAction}
              onClick={() => onAction(project, "close")}
            />
            <ActionButton
              actionKey={`archive:${project.id}`}
              disabled={!canArchiveProject(project.status)}
              icon={<Archive className="h-3.5 w-3.5" />}
              label="Archive"
              runningAction={runningAction}
              variant="danger"
              onClick={() => onAction(project, "archive")}
            />
          </div>
        </div>
      </PanelBody>
    </Panel>
  );
}

function ActionButton({
  actionKey,
  disabled,
  icon,
  label,
  runningAction,
  variant = "outline",
  onClick,
}: {
  actionKey: string;
  disabled: boolean;
  icon: React.ReactNode;
  label: string;
  runningAction: string | null;
  variant?: "outline" | "danger";
  onClick: () => Promise<void>;
}) {
  const isRunning = runningAction === actionKey;

  return (
    <Button size="sm" variant={variant} disabled={disabled || !!runningAction} onClick={() => void onClick()}>
      {isRunning ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : icon}
      {label}
    </Button>
  );
}

function Capability({ label, enabled }: { label: string; enabled: boolean }) {
  return (
    <div className="flex items-center justify-between rounded-md border border-border px-3 py-2">
      <span>{label}</span>
      <span className={enabled ? "inline-flex items-center gap-1 text-emerald-700" : "inline-flex items-center gap-1 text-muted-foreground"}>
        {enabled ? <CheckCircle2 className="h-3.5 w-3.5" /> : <Lock className="h-3.5 w-3.5" />}
        {enabled ? "Enabled" : "Locked"}
      </span>
    </div>
  );
}
