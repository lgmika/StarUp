"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowUpRight,
  BarChart3,
  FileCheck2,
  Handshake,
  KeyRound,
  TrendingUp,
  UserRound,
  Wallet,
} from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/workspace/status-badge";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { queryKeys } from "@/lib/query-keys";
import { investorService } from "@/services";
import { InlineError } from "@/components/ui/error-boundary";

const metricColors = [
  { bg: "bg-emerald-50", text: "text-emerald-600", ring: "ring-emerald-100" },
  { bg: "bg-amber-50", text: "text-amber-600", ring: "ring-amber-100" },
  { bg: "bg-blue-50", text: "text-blue-600", ring: "ring-blue-100" },
  { bg: "bg-purple-50", text: "text-purple-600", ring: "ring-purple-100" },
];

const links = [
  {
    href: "/investor/profile",
    title: "Investor profile",
    description: "Update investment identity and focus.",
    icon: UserRound,
  },
  {
    href: "/investor/projects",
    title: "Discover projects",
    description: "Review investor-visible opportunities.",
    icon: BarChart3,
  },
  {
    href: "/investor/interests",
    title: "My interests",
    description: "Track founder responses and access.",
    icon: Handshake,
  },
];

export default function InvestorPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Investor, SystemRoles.Admin]}>
      <InvestorDashboard />
    </RoleGuard>
  );
}

function InvestorDashboard() {
  const dashboardQuery = useQuery({
    queryKey: [...queryKeys.dashboard, "investor"],
    queryFn: investorService.getDashboard,
  });

  if (dashboardQuery.isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-24 rounded-2xl" />
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-28 rounded-2xl" />
          ))}
        </div>
        <Skeleton className="h-48 rounded-2xl" />
      </div>
    );
  }

  const dashboard = dashboardQuery.data;

  const metrics = [
    { icon: Handshake, label: "Interested projects", value: dashboard?.interestedProjects ?? 0 },
    { icon: FileCheck2, label: "NDA pending", value: dashboard?.ndaPending ?? 0 },
    { icon: KeyRound, label: "Accepted access", value: dashboard?.acceptedAccess ?? 0 },
    { icon: BarChart3, label: "Saved projects", value: dashboard?.savedProjects ?? 0 },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <section className="relative overflow-hidden rounded-2xl border border-border/60 bg-gradient-to-r from-emerald-500/[0.04] via-card to-card p-6 shadow-sm">
        <div className="pointer-events-none absolute -right-16 -top-16 h-40 w-40 rounded-full bg-emerald-500/5 blur-3xl" />
        <div className="relative">
          <div className="flex items-center gap-2">
            <Wallet className="h-4 w-4 text-emerald-600" />
            <p className="text-sm font-medium text-muted-foreground">
              Investment Portal
            </p>
          </div>
          <h1 className="mt-2 text-2xl font-bold tracking-tight">
            Investor Dashboard
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Portfolio access, interest metrics, and discovery.
          </p>
        </div>
      </section>

      {dashboardQuery.error ? (
        <InlineError
          message={getApiErrorMessage(dashboardQuery.error)}
          onRetry={() => void dashboardQuery.refetch()}
        />
      ) : null}

      {/* Metric cards */}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {metrics.map((metric, index) => {
          const Icon = metric.icon;
          const color = metricColors[index];
          return (
            <div
              key={metric.label}
              className="rounded-2xl border border-border/60 bg-card p-5 shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-md"
            >
              <div className="flex items-center justify-between">
                <div className={`flex h-10 w-10 items-center justify-center rounded-xl ${color.bg} ${color.text} ring-1 ${color.ring}`}>
                  <Icon className="h-5 w-5" />
                </div>
                <TrendingUp className="h-3.5 w-3.5 text-muted-foreground/40" />
              </div>
              <p className="mt-4 text-3xl font-bold tracking-tight">{metric.value}</p>
              <p className="mt-1 text-sm font-medium text-foreground">{metric.label}</p>
            </div>
          );
        })}
      </div>

      <div className="grid gap-4 lg:grid-cols-[1fr_360px]">
        {/* Interest status */}
        <div className="rounded-2xl border border-border/60 bg-card shadow-sm">
          <div className="border-b border-border p-4">
            <h2 className="text-sm font-bold">Interest Status Breakdown</h2>
          </div>
          <div className="space-y-1 p-3">
            {dashboard?.interestStatus.length ? (
              dashboard.interestStatus.map((item) => (
                <div
                  key={item.status}
                  className="flex items-center justify-between rounded-xl p-3 transition-colors hover:bg-muted/50"
                >
                  <StatusBadge value={item.status} />
                  <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-muted text-xs font-bold">
                    {item.count}
                  </span>
                </div>
              ))
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">
                No investor interests yet.
              </p>
            )}
          </div>
        </div>

        {/* Quick links */}
        <div className="space-y-2">
          {links.map((item) => {
            const Icon = item.icon;
            return (
              <Link
                key={item.href}
                href={item.href}
                className="group flex items-center gap-3 rounded-2xl border border-border/60 bg-card p-4 shadow-sm transition-all hover:-translate-y-0.5 hover:shadow-md"
              >
                <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground">
                  <Icon className="h-4 w-4" />
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block text-sm font-semibold">{item.title}</span>
                  <span className="block text-xs text-muted-foreground">{item.description}</span>
                </span>
                <ArrowUpRight className="h-4 w-4 shrink-0 text-muted-foreground/40 transition-transform group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
              </Link>
            );
          })}
        </div>
      </div>
    </div>
  );
}
