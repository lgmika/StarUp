"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import {
  BarChart3,
  Bookmark,
  FileCheck2,
  Handshake,
  MousePointerClick,
  Send,
  TrendingUp,
  UsersRound,
} from "lucide-react";
import { ProjectWorkspaceNav } from "@/components/projects/project-workspace-nav";
import { InlineError } from "@/components/ui/error-boundary";
import { Skeleton } from "@/components/ui/skeleton";
import { getApiErrorMessage } from "@/lib/api";
import { projectService } from "@/services/project-service";

const metricColors = [
  { bg: "bg-blue-50", text: "text-blue-600", ring: "ring-blue-100" },
  { bg: "bg-amber-50", text: "text-amber-600", ring: "ring-amber-100" },
  { bg: "bg-emerald-50", text: "text-emerald-600", ring: "ring-emerald-100" },
  { bg: "bg-purple-50", text: "text-purple-600", ring: "ring-purple-100" },
  { bg: "bg-rose-50", text: "text-rose-600", ring: "ring-rose-100" },
  { bg: "bg-teal-50", text: "text-teal-600", ring: "ring-teal-100" },
];

export default function ProjectDashboardPage() {
  const { id } = useParams<{ id: string }>();

  const query = useQuery({
    queryKey: ["project-dashboard", id],
    queryFn: () => projectService.getProjectDashboard(id),
  });

  if (query.isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-16 w-full rounded-2xl" />
        <Skeleton className="h-24 w-full rounded-2xl" />
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-28 rounded-2xl" />
          ))}
        </div>
        <div className="grid gap-4 lg:grid-cols-2">
          <Skeleton className="h-48 rounded-2xl" />
          <Skeleton className="h-48 rounded-2xl" />
        </div>
      </div>
    );
  }

  if (query.error || !query.data) {
    return (
      <div className="space-y-6">
        <ProjectWorkspaceNav projectId={id} />
        <InlineError
          message={getApiErrorMessage(query.error)}
          onRetry={() => void query.refetch()}
        />
      </div>
    );
  }

  const data = query.data;

  const metrics = [
    { label: "Views", value: data.projectViews, icon: MousePointerClick },
    { label: "Saved", value: data.savedCount, icon: Bookmark },
    { label: "Applications", value: data.applications, icon: Send },
    { label: "Team members", value: data.teamSize, icon: UsersRound },
    { label: "Investor interests", value: data.investorInterests, icon: Handshake },
    { label: "NDA records", value: data.ndaAgreements, icon: FileCheck2 },
  ] as const;

  return (
    <div className="space-y-6">
      <ProjectWorkspaceNav projectId={id} />

      {/* Header */}
      <section className="relative overflow-hidden rounded-2xl border border-border/60 bg-gradient-to-r from-primary/[0.04] via-card to-card p-6 shadow-sm">
        <div className="pointer-events-none absolute -right-16 -top-16 h-40 w-40 rounded-full bg-primary/5 blur-3xl" />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <div className="flex items-center gap-2">
              <BarChart3 className="h-4 w-4 text-primary" />
              <p className="text-sm font-medium text-muted-foreground">
                Workspace
              </p>
            </div>
            <h1 className="mt-2 text-2xl font-bold tracking-tight">
              {data.projectTitle}
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Founder metrics and project operations from the live backend.
            </p>
          </div>
          <div className="flex items-center gap-3 rounded-xl border border-border/60 bg-card p-4 shadow-sm">
            <div>
              <p className="text-3xl font-bold tracking-tight">
                {data.applicationConversionRate.toFixed(1)}%
              </p>
              <p className="text-xs font-medium text-muted-foreground">
                Conversion rate
              </p>
            </div>
            <TrendingUp className="h-8 w-8 text-emerald-500" />
          </div>
        </div>
      </section>

      {/* Metrics Grid */}
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {metrics.map((metric, index) => {
          const Icon = metric.icon;
          const color = metricColors[index % metricColors.length];
          return (
            <div
              key={metric.label}
              className="rounded-2xl border border-border/60 bg-card p-5 shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-md"
            >
              <div className="flex items-center justify-between">
                <div
                  className={`flex h-10 w-10 items-center justify-center rounded-xl ${color.bg} ${color.text} ring-1 ${color.ring}`}
                >
                  <Icon className="h-5 w-5" />
                </div>
                <TrendingUp className="h-3.5 w-3.5 text-muted-foreground/40" />
              </div>
              <p className="mt-4 text-3xl font-bold tracking-tight">
                {metric.value}
              </p>
              <p className="mt-1 text-sm font-medium text-foreground">
                {metric.label}
              </p>
            </div>
          );
        })}
      </div>

      {/* Status Breakdown Panels */}
      <div className="grid gap-4 lg:grid-cols-2">
        <StatusPanel
          title="Applications by status"
          rows={data.applicationsByStatus}
        />
        <StatusPanel
          title="Investor interests by status"
          rows={data.investorInterestsByStatus}
        />
      </div>
    </div>
  );
}

function StatusPanel({
  title,
  rows,
}: {
  title: string;
  rows: Array<{ status: string; count: number }>;
}) {
  return (
    <div className="rounded-2xl border border-border/60 bg-card shadow-sm">
      <div className="border-b border-border p-4">
        <h2 className="text-sm font-bold">{title}</h2>
      </div>
      <div className="space-y-1 p-3">
        {rows.length ? (
          rows.map((row) => (
            <div
              key={row.status}
              className="flex items-center justify-between rounded-xl p-3 transition-colors hover:bg-muted/50"
            >
              <span className="text-sm font-medium">{row.status}</span>
              <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-muted text-xs font-bold">
                {row.count}
              </span>
            </div>
          ))
        ) : (
          <p className="py-8 text-center text-sm text-muted-foreground">
            No data yet.
          </p>
        )}
      </div>
    </div>
  );
}
