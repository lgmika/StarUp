"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { AlertCircle, ArrowLeft, Briefcase, LockKeyhole, Target, type LucideIcon } from "lucide-react";
import { ProjectStageBadge, ProjectStatusBadge, ProjectVisibilityBadge } from "@/components/projects/project-badges";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { projectService } from "@/services";
import type { ProjectDetailDto } from "@/types/project";

type LoadState = "loading" | "ready" | "forbidden" | "not-found" | "error";

export default function ProjectDetailPage() {
  const params = useParams<{ id: string }>();
  const [project, setProject] = useState<ProjectDetailDto | null>(null);
  const [state, setState] = useState<LoadState>("loading");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadProject() {
      setState("loading");
      setError(null);
      try {
        const detail = await projectService.getProject(params.id);
        setProject(detail);
        setState("ready");
      } catch (loadError) {
        const status = getHttpStatus(loadError);
        setProject(null);
        if (status === 403) {
          setError(getApiErrorMessage(loadError));
          setState("forbidden");
        } else if (status === 404) {
          setError(getApiErrorMessage(loadError));
          setState("not-found");
        } else {
          setError(getApiErrorMessage(loadError));
          setState("error");
        }
      }
    }

    void loadProject();
  }, [params.id]);

  return (
    <main className="min-h-screen bg-background">
      <header className="border-b border-border bg-card">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between gap-3 px-4 sm:px-6 lg:px-8">
          <Link className="inline-flex items-center gap-2 text-sm font-medium text-muted-foreground hover:text-foreground" href="/projects">
            <ArrowLeft className="h-4 w-4" />
            Back to projects
          </Link>
          <Link className="text-sm font-medium text-primary hover:underline" href="/auth/login">
            Sign in
          </Link>
        </div>
      </header>

      <section className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
        {state === "loading" ? <ProjectDetailSkeleton /> : null}
        {state === "forbidden" ? <ProtectedProjectState message={error} /> : null}
        {state === "not-found" ? <InfoState title="Project not found" message={error ?? "This project does not exist or was removed."} /> : null}
        {state === "error" ? <InfoState title="Could not load project" message={error ?? "Please try again."} tone="error" /> : null}
        {state === "ready" && project ? <ProjectDetail project={project} /> : null}
      </section>
    </main>
  );
}

function ProjectDetail({ project }: { project: ProjectDetailDto }) {
  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_360px]">
      <section className="space-y-6">
        <div className="rounded-lg border border-border bg-card p-5 shadow-sm">
          <div className="flex flex-wrap gap-2">
            <ProjectStatusBadge status={project.status} />
            <ProjectStageBadge stage={project.stage} />
            <ProjectVisibilityBadge visibility={project.visibility} />
            {project.isRecruiting ? <Badge tone="success">Recruiting</Badge> : null}
            {project.requiresNda ? <Badge tone="warning">NDA required</Badge> : null}
          </div>
          <h1 className="mt-4 text-2xl font-semibold">{project.title}</h1>
          <p className="mt-3 max-w-3xl text-sm leading-6 text-muted-foreground">{project.summary}</p>
          {project.isRecruiting ? (
            <Link
              className="mt-5 inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
              href={`/projects/${project.id}/apply`}
            >
              Apply to project
            </Link>
          ) : null}
        </div>

        <DetailSection icon={Target} title="Problem" text={project.problem} />
        <DetailSection icon={Briefcase} title="Solution" text={project.solution} />

        <div className="grid gap-4 md:grid-cols-2">
          <OptionalDetail title="Target market" text={project.targetMarket} />
          <OptionalDetail title="Business model" text={project.businessModel} />
          <OptionalDetail title="Funding needs" text={project.fundingNeeds} />
          <OptionalDetail title="Pitch deck" text={project.pitchDeckUrl} isLink />
        </div>
      </section>

      <aside className="space-y-4">
        {project.requiresNda ? <NdaPrompt /> : null}

        <Panel>
          <PanelHeader>
            <PanelTitle>Required roles</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-3">
            {project.requiredRoles.length ? (
              project.requiredRoles.map((role) => (
                <div key={role.id} className="rounded-md border border-border p-3">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold">{role.roleName}</p>
                    <Badge tone={role.isOpen ? "success" : "muted"}>{role.isOpen ? "Open" : "Closed"}</Badge>
                  </div>
                  <p className="mt-1 text-xs text-muted-foreground">{role.slots} slot{role.slots === 1 ? "" : "s"}</p>
                  {role.description ? <p className="mt-2 text-sm text-muted-foreground">{role.description}</p> : null}
                </div>
              ))
            ) : (
              <EmptyPanelText text="No required roles returned by the backend." />
            )}
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader>
            <PanelTitle>Required skills</PanelTitle>
          </PanelHeader>
          <PanelBody>
            {project.requiredSkills.length ? (
              <div className="flex flex-wrap gap-2">
                {project.requiredSkills.map((skill) => (
                  <Badge key={skill.id} tone="muted">
                    {skill.name}
                  </Badge>
                ))}
              </div>
            ) : (
              <EmptyPanelText text="No required skills returned by the backend." />
            )}
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader>
            <PanelTitle>Metadata</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-2 text-sm text-muted-foreground">
            <p>Slug: {project.slug}</p>
            <p>Created: {new Date(project.createdAt).toLocaleString()}</p>
            {project.updatedAt ? <p>Updated: {new Date(project.updatedAt).toLocaleString()}</p> : null}
          </PanelBody>
        </Panel>
      </aside>
    </div>
  );
}

function DetailSection({ icon: Icon, title, text }: { icon: LucideIcon; title: string; text: string }) {
  return (
    <Panel>
      <PanelHeader className="flex flex-row items-center gap-2">
        <Icon className="h-4 w-4 text-muted-foreground" />
        <PanelTitle>{title}</PanelTitle>
      </PanelHeader>
      <PanelBody>
        <p className="text-sm leading-6 text-muted-foreground">{text}</p>
      </PanelBody>
    </Panel>
  );
}

function OptionalDetail({ title, text, isLink = false }: { title: string; text?: string; isLink?: boolean }) {
  return (
    <Panel>
      <PanelHeader>
        <PanelTitle>{title}</PanelTitle>
      </PanelHeader>
      <PanelBody>
        {text ? (
          isLink ? (
            <a className="break-all text-sm font-medium text-primary hover:underline" href={text} rel="noreferrer" target="_blank">
              {text}
            </a>
          ) : (
            <p className="text-sm leading-6 text-muted-foreground">{text}</p>
          )
        ) : (
          <EmptyPanelText text="Not returned by backend." />
        )}
      </PanelBody>
    </Panel>
  );
}

function NdaPrompt() {
  return (
    <Panel className="border-amber-200 bg-amber-50 text-amber-950">
      <PanelBody className="space-y-3">
        <div className="flex items-center gap-2 font-semibold">
          <LockKeyhole className="h-4 w-4" />
          NDA required
        </div>
        <p className="text-sm leading-6">
          Backend returned this project detail and marked it as NDA-required. Future NDA phase will connect the current NDA template and acceptance flow.
        </p>
      </PanelBody>
    </Panel>
  );
}

function ProtectedProjectState({ message }: { message: string | null }) {
  return (
    <Panel>
      <PanelBody className="mx-auto flex max-w-xl flex-col items-center py-14 text-center">
        <div className="flex h-12 w-12 items-center justify-center rounded-md bg-amber-100 text-amber-700">
          <LockKeyhole className="h-6 w-6" />
        </div>
        <h1 className="mt-5 text-xl font-semibold">Protected project</h1>
        <p className="mt-2 text-sm leading-6 text-muted-foreground">
          {message ?? "The backend did not return project details for this account. It may require membership, investor access, or an accepted NDA."}
        </p>
        <div className="mt-6 flex flex-col gap-2 sm:flex-row">
          <Link
            className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
            href="/auth/login"
          >
            Sign in with another account
          </Link>
          <Link
            className="inline-flex h-10 items-center justify-center rounded-md border border-border bg-background px-4 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground"
            href="/projects"
          >
            Back to discovery
          </Link>
        </div>
      </PanelBody>
    </Panel>
  );
}

function InfoState({ title, message, tone = "default" }: { title: string; message: string; tone?: "default" | "error" }) {
  return (
    <Panel>
      <PanelBody className="mx-auto flex max-w-xl flex-col items-center py-14 text-center">
        <div className={tone === "error" ? "text-destructive" : "text-muted-foreground"}>
          <AlertCircle className="h-8 w-8" />
        </div>
        <h1 className="mt-5 text-xl font-semibold">{title}</h1>
        <p className="mt-2 text-sm leading-6 text-muted-foreground">{message}</p>
      </PanelBody>
    </Panel>
  );
}

function ProjectDetailSkeleton() {
  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_360px]">
      <div className="space-y-4">
        <div className="h-44 animate-pulse rounded-lg bg-muted" />
        <div className="h-36 animate-pulse rounded-lg bg-muted" />
        <div className="h-36 animate-pulse rounded-lg bg-muted" />
      </div>
      <div className="space-y-4">
        <div className="h-40 animate-pulse rounded-lg bg-muted" />
        <div className="h-40 animate-pulse rounded-lg bg-muted" />
      </div>
    </div>
  );
}

function EmptyPanelText({ text }: { text: string }) {
  return <p className="text-sm text-muted-foreground">{text}</p>;
}

function getHttpStatus(error: unknown) {
  if (typeof error === "object" && error !== null && "response" in error) {
    const response = (error as { response?: { status?: number } }).response;
    return response?.status;
  }
  return undefined;
}
