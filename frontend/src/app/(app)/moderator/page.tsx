"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { EyeOff, FileText, ListChecks, ShieldCheck, XCircle } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { moderatorService } from "@/services";
import type { ModeratorDashboardDto } from "@/types/moderator";

const metricConfig = [
  { key: "pendingProjects", label: "Pending", icon: ListChecks },
  { key: "publishedProjects", label: "Published", icon: ShieldCheck },
  { key: "rejectedProjects", label: "Rejected", icon: XCircle },
  { key: "hiddenProjects", label: "Hidden", icon: EyeOff },
  { key: "pendingReports", label: "Pending reports", icon: FileText },
] as const;

export default function ModeratorPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Moderator, SystemRoles.Admin]}>
      <ModeratorDashboard />
    </RoleGuard>
  );
}

function ModeratorDashboard() {
  const [dashboard, setDashboard] = useState<ModeratorDashboardDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function loadDashboard() {
      try {
        setDashboard(await moderatorService.getDashboard());
      } catch (loadError) {
        setError(getApiErrorMessage(loadError));
      } finally {
        setIsLoading(false);
      }
    }

    void loadDashboard();
  }, []);

  if (isLoading) return <LoadingState label="Loading moderator dashboard" />;

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold">Moderator Dashboard</h1>
        <p className="mt-2 text-sm text-muted-foreground">Review queue metrics are loaded from backend moderation APIs.</p>
      </div>
      {error ? <p className="text-sm text-destructive">{error}</p> : null}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        {metricConfig.map((metric) => {
          const Icon = metric.icon;
          return (
            <Panel key={metric.key}>
              <PanelBody>
                <Icon className="h-5 w-5 text-muted-foreground" />
                <p className="mt-4 text-2xl font-semibold">{dashboard?.[metric.key] ?? 0}</p>
                <p className="mt-1 text-sm text-muted-foreground">{metric.label}</p>
              </PanelBody>
            </Panel>
          );
        })}
      </div>
      <Panel>
        <PanelHeader>
          <PanelTitle>Review work</PanelTitle>
        </PanelHeader>
        <PanelBody className="flex flex-wrap gap-2">
          <Link className="inline-flex h-10 items-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90" href="/moderator/projects/pending">
            Open pending queue
          </Link>
          <Link className="inline-flex h-10 items-center rounded-md border border-border px-4 text-sm font-medium hover:bg-accent" href="/moderator/reports">
            Reports placeholder
          </Link>
        </PanelBody>
      </Panel>
    </div>
  );
}
