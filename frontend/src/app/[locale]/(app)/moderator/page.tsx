"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  ArrowUpRight,
  EyeOff,
  FileText,
  ListChecks,
  Shield,
  ShieldCheck,
  TrendingUp,
  XCircle,
} from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { Skeleton } from "@/components/ui/skeleton";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { moderatorService } from "@/services";
import type { ModeratorDashboardDto } from "@/types/moderator";
import { InlineError } from "@/components/ui/error-boundary";

const metricColors = [
  { bg: "bg-amber-50", text: "text-amber-600", ring: "ring-amber-100" },
  { bg: "bg-emerald-50", text: "text-emerald-600", ring: "ring-emerald-100" },
  { bg: "bg-rose-50", text: "text-rose-600", ring: "ring-rose-100" },
  { bg: "bg-slate-50", text: "text-slate-600", ring: "ring-slate-100" },
  { bg: "bg-orange-50", text: "text-orange-600", ring: "ring-orange-100" },
];

const metricConfig = [
  { key: "pendingProjects", label: "Pending review", icon: ListChecks },
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

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-24 rounded-2xl" />
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-28 rounded-2xl" />
          ))}
        </div>
        <Skeleton className="h-32 rounded-2xl" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <section className="relative overflow-hidden rounded-2xl border border-border/60 bg-gradient-to-r from-amber-500/[0.04] via-card to-card p-6 shadow-sm">
        <div className="pointer-events-none absolute -right-16 -top-16 h-40 w-40 rounded-full bg-amber-500/5 blur-3xl" />
        <div className="relative">
          <div className="flex items-center gap-2">
            <Shield className="h-4 w-4 text-amber-600" />
            <p className="text-sm font-medium text-muted-foreground">
              Content Moderation
            </p>
          </div>
          <h1 className="mt-2 text-2xl font-bold tracking-tight">
            Moderator Dashboard
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Review queue metrics and moderation workflows.
          </p>
        </div>
      </section>

      {error ? <InlineError message={error} onRetry={() => window.location.reload()} /> : null}

      {/* Metric cards */}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        {metricConfig.map((metric, index) => {
          const Icon = metric.icon;
          const color = metricColors[index];
          return (
            <div
              key={metric.key}
              className="rounded-2xl border border-border/60 bg-card p-4 shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-md"
            >
              <div className="flex items-center justify-between">
                <div className={`flex h-9 w-9 items-center justify-center rounded-lg ${color.bg} ${color.text} ring-1 ${color.ring}`}>
                  <Icon className="h-4 w-4" />
                </div>
                <TrendingUp className="h-3.5 w-3.5 text-muted-foreground/40" />
              </div>
              <p className="mt-3 text-2xl font-bold tracking-tight">
                {dashboard?.[metric.key] ?? 0}
              </p>
              <p className="mt-0.5 text-xs font-medium text-muted-foreground">
                {metric.label}
              </p>
            </div>
          );
        })}
      </div>

      {/* Quick actions */}
      <div className="rounded-2xl border border-border/60 bg-card shadow-sm">
        <div className="border-b border-border p-4">
          <h2 className="text-sm font-bold">Review Work</h2>
        </div>
        <div className="grid gap-1 p-3 sm:grid-cols-2">
          <Link
            href="/moderator/projects/pending"
            className="group flex items-center gap-3 rounded-xl p-4 transition-colors hover:bg-accent"
          >
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary text-primary-foreground shadow-sm">
              <ListChecks className="h-4 w-4" />
            </span>
            <span className="min-w-0 flex-1">
              <span className="block text-sm font-semibold">Pending Queue</span>
              <span className="block text-xs text-muted-foreground">
                Review and approve new projects
              </span>
            </span>
            <ArrowUpRight className="h-4 w-4 shrink-0 text-muted-foreground/40 transition-transform group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
          </Link>
          <Link
            href="/moderator/reports"
            className="group flex items-center gap-3 rounded-xl p-4 transition-colors hover:bg-accent"
          >
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground">
              <FileText className="h-4 w-4" />
            </span>
            <span className="min-w-0 flex-1">
              <span className="block text-sm font-semibold">Reports Queue</span>
              <span className="block text-xs text-muted-foreground">
                Investigate and resolve reports
              </span>
            </span>
            <ArrowUpRight className="h-4 w-4 shrink-0 text-muted-foreground/40 transition-transform group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
          </Link>
        </div>
      </div>
    </div>
  );
}
