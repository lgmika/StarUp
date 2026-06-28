"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { ClipboardList, ExternalLink, RefreshCw } from "lucide-react";
import { ApplicationStatusBadge } from "@/components/applications/application-status-badge";
import { AuthMessage } from "@/components/auth/auth-message";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { applicationService } from "@/services";
import type { ApplicationDto } from "@/types/application";

export default function ApplicationsPage() {
  const [applications, setApplications] = useState<ApplicationDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function loadApplications() {
    setIsLoading(true);
    setError(null);
    try {
      setApplications(await applicationService.listMyApplications());
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadApplications();
  }, []);

  if (isLoading) return <LoadingState label="Loading applications" />;

  return (
    <div className="space-y-5">
      <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
        <div>
          <h1 className="text-2xl font-semibold">My Applications</h1>
          <p className="mt-2 text-sm text-muted-foreground">Track applications submitted from your account.</p>
        </div>
        <Button variant="outline" onClick={() => void loadApplications()}>
          <RefreshCw className="h-4 w-4" />
          Refresh
        </Button>
      </div>

      {error ? <AuthMessage tone="error">{error}</AuthMessage> : null}

      {applications.length === 0 ? (
        <Panel>
          <PanelBody className="py-12 text-center">
            <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-md bg-muted text-muted-foreground">
              <ClipboardList className="h-5 w-5" />
            </div>
            <h2 className="mt-4 text-base font-semibold">No applications yet</h2>
            <p className="mt-2 text-sm text-muted-foreground">Discover recruiting projects and apply when you are ready.</p>
            <Link className="mt-5 inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90" href="/projects">
              Discover projects
            </Link>
          </PanelBody>
        </Panel>
      ) : (
        <div className="grid gap-3">
          {applications.map((application) => (
            <Panel key={application.id}>
              <PanelBody className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="text-base font-semibold">{application.projectTitle}</h2>
                    <ApplicationStatusBadge status={application.status} />
                    {application.cvTitle ? <Badge tone="muted">{application.cvTitle}</Badge> : null}
                  </div>
                  <p className="mt-2 line-clamp-2 text-sm leading-6 text-muted-foreground">{application.coverLetter}</p>
                  <p className="mt-2 text-xs text-muted-foreground">Submitted {new Date(application.createdAt).toLocaleString()}</p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <Link className="inline-flex h-9 items-center justify-center gap-2 rounded-md border border-border bg-background px-3 text-sm font-medium hover:bg-accent" href={`/applications/${application.id}?projectId=${application.projectId}`}>
                    <ExternalLink className="h-4 w-4" />
                    Detail
                  </Link>
                  <Link className="inline-flex h-9 items-center justify-center gap-2 rounded-md border border-border bg-background px-3 text-sm font-medium hover:bg-accent" href={`/projects/${application.projectId}`}>
                    Project
                  </Link>
                </div>
              </PanelBody>
            </Panel>
          ))}
        </div>
      )}
    </div>
  );
}
