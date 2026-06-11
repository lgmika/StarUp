"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { Activity, AlertCircle, Loader2, Search, X } from "lucide-react";
import { ProjectCard } from "@/components/projects/project-card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { projectService } from "@/services";
import type { ProjectSummaryDto } from "@/types/project";

type LoadState = "idle" | "loading" | "ready" | "error";

export default function ProjectDiscoveryPage() {
  const [projects, setProjects] = useState<ProjectSummaryDto[]>([]);
  const [query, setQuery] = useState("");
  const [submittedQuery, setSubmittedQuery] = useState("");
  const [state, setState] = useState<LoadState>("idle");
  const [error, setError] = useState<string | null>(null);

  const resultLabel = useMemo(() => {
    if (state === "loading") return "Loading";
    if (projects.length === 1) return "1 project";
    return `${projects.length} projects`;
  }, [projects.length, state]);

  async function loadProjects(search = "") {
    setState("loading");
    setError(null);
    try {
      const nextProjects = await projectService.listProjects(search);
      setProjects(nextProjects);
      setState("ready");
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
      setProjects([]);
      setState("error");
    }
  }

  function submitSearch(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextQuery = query.trim();
    setSubmittedQuery(nextQuery);
    void loadProjects(nextQuery);
  }

  function clearSearch() {
    setQuery("");
    setSubmittedQuery("");
    void loadProjects("");
  }

  useEffect(() => {
    void loadProjects();
  }, []);

  return (
    <main className="min-h-screen bg-background">
      <header className="border-b border-border bg-card">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between gap-3 px-4 sm:px-6 lg:px-8">
          <Link className="flex min-w-0 items-center gap-3" href="/">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary text-primary-foreground">
              <Activity className="h-5 w-5" />
            </span>
            <span className="min-w-0">
              <span className="block truncate text-sm font-semibold">StartupConnect</span>
              <span className="block truncate text-xs text-muted-foreground">Project discovery</span>
            </span>
          </Link>
          <div className="flex items-center gap-2">
            <Link className="text-sm font-medium text-muted-foreground hover:text-foreground" href="/auth/login">
              Sign in
            </Link>
            <Link
              className="inline-flex h-9 items-center justify-center rounded-md bg-primary px-3 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
              href="/auth/register"
            >
              Join
            </Link>
          </div>
        </div>
      </header>

      <section className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
        <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div>
            <h1 className="text-2xl font-semibold">Discover projects</h1>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">
              Browse public project summaries returned by the backend. Private or protected details are only shown when backend APIs return them.
            </p>
          </div>
          <Badge tone="muted">{resultLabel}</Badge>
        </div>

        <form className="mt-6 flex flex-col gap-3 rounded-lg border border-border bg-card p-3 shadow-sm sm:flex-row" onSubmit={submitSearch}>
          <div className="relative flex-1">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              className="pl-9"
              placeholder="Search by keyword"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
            />
          </div>
          <div className="flex gap-2">
            {submittedQuery ? (
              <Button type="button" variant="outline" onClick={clearSearch}>
                <X className="h-4 w-4" />
                Clear
              </Button>
            ) : null}
            <Button type="submit" disabled={state === "loading"}>
              {state === "loading" ? <Loader2 className="h-4 w-4 animate-spin" /> : <Search className="h-4 w-4" />}
              Search
            </Button>
          </div>
        </form>

        {submittedQuery ? (
          <p className="mt-3 text-sm text-muted-foreground">
            Showing results for <span className="font-medium text-foreground">{submittedQuery}</span>
          </p>
        ) : null}

        <div className="mt-6">
          {state === "loading" ? <ProjectLoadingGrid /> : null}

          {state === "error" ? (
            <Panel>
              <PanelBody className="flex items-start gap-3 text-sm text-destructive">
                <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
                <div>
                  <p className="font-medium">Could not load projects</p>
                  <p className="mt-1 text-destructive/80">{error}</p>
                </div>
              </PanelBody>
            </Panel>
          ) : null}

          {state === "ready" && projects.length === 0 ? (
            <Panel>
              <PanelBody className="py-12 text-center">
                <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-md bg-muted text-muted-foreground">
                  <Search className="h-5 w-5" />
                </div>
                <h2 className="mt-4 text-base font-semibold">No projects found</h2>
                <p className="mt-2 text-sm text-muted-foreground">
                  {submittedQuery ? "Try another keyword or clear the search." : "No published projects are available yet."}
                </p>
              </PanelBody>
            </Panel>
          ) : null}

          {state === "ready" && projects.length > 0 ? (
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {projects.map((project) => (
                <ProjectCard key={project.id} project={project} />
              ))}
            </div>
          ) : null}
        </div>
      </section>
    </main>
  );
}

function ProjectLoadingGrid() {
  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      {Array.from({ length: 6 }).map((_, index) => (
        <Panel key={index}>
          <PanelBody className="space-y-4">
            <div className="flex gap-2">
              <div className="h-6 w-20 animate-pulse rounded-md bg-muted" />
              <div className="h-6 w-16 animate-pulse rounded-md bg-muted" />
            </div>
            <div className="h-5 w-2/3 animate-pulse rounded-md bg-muted" />
            <div className="space-y-2">
              <div className="h-4 animate-pulse rounded-md bg-muted" />
              <div className="h-4 w-5/6 animate-pulse rounded-md bg-muted" />
              <div className="h-4 w-3/5 animate-pulse rounded-md bg-muted" />
            </div>
          </PanelBody>
        </Panel>
      ))}
    </div>
  );
}
