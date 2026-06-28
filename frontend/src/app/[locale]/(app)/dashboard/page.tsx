"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  ArrowUpRight,
  Bell,
  BriefcaseBusiness,
  CalendarClock,
  FileText,
  Folders,
  Heart,
  ShieldCheck,
  Sparkles,
  TrendingUp,
  Search,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/workspace/empty-state";
import { StatusBadge } from "@/components/workspace/status-badge";
import { getPrimaryRole } from "@/lib/permissions";
import { activityService, dashboardService } from "@/services";
import { useAuthStore } from "@/stores/auth-store";
import type { ActivityDto } from "@/types/activity";
import type { UserDashboardDto } from "@/types/dashboard";
import { InlineError } from "@/components/ui/error-boundary";

const quickLinks = [
  { title: "Complete profile", href: "/profile", icon: FileText, description: "Update your bio and skills" },
  { title: "My projects", href: "/projects/me/owned", icon: Folders, description: "Manage your startup projects" },
  { title: "Discover projects", href: "/projects", icon: Search, description: "Browse open projects and founders" },
];

const dateFormatter = new Intl.DateTimeFormat("en-US", {
  dateStyle: "medium",
  timeStyle: "short",
});

const metricColors = [
  { bg: "bg-blue-50", text: "text-blue-600", ring: "ring-blue-100" },
  { bg: "bg-purple-50", text: "text-purple-600", ring: "ring-purple-100" },
  { bg: "bg-emerald-50", text: "text-emerald-600", ring: "ring-emerald-100" },
  { bg: "bg-rose-50", text: "text-rose-600", ring: "ring-rose-100" },
];

export default function DashboardPage() {
  const user = useAuthStore((state) => state.user);
  const primaryRole = getPrimaryRole(user?.roles ?? []);
  const [dashboard, setDashboard] = useState<UserDashboardDto | null>(null);
  const [feed, setFeed] = useState<ActivityDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const metrics = useMemo(() => {
    if (!dashboard) return [];

    return [
      { label: "Applications", value: dashboard.applications, hint: "Total applications", icon: BriefcaseBusiness },
      { label: "Upcoming interviews", value: dashboard.upcomingInterviews, hint: "Needs attention", icon: CalendarClock },
      { label: "Joined projects", value: dashboard.joinedProjects, hint: "Active memberships", icon: Folders },
      { label: "Saved projects", value: dashboard.savedProjects, hint: "For later review", icon: Heart },
    ];
  }, [dashboard]);

  useEffect(() => {
    async function loadDashboard() {
      try {
        setError(null);
        setIsLoading(true);
        const [nextDashboard, nextFeed] = await Promise.all([
          dashboardService.getMine({ timezoneOffsetMinutes: new Date().getTimezoneOffset() }),
          activityService.getFeed(1, 6),
        ]);
        setDashboard(nextDashboard);
        setFeed(nextFeed.items);
      } catch {
        setError("Could not load dashboard data. Please check your session and backend connection.");
      } finally {
        setIsLoading(false);
      }
    }

    void loadDashboard();
  }, []);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-28 w-full rounded-2xl" />
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-32 rounded-2xl" />
          ))}
        </div>
        <div className="grid gap-4 lg:grid-cols-[1fr_360px]">
          <Skeleton className="h-64 rounded-2xl" />
          <Skeleton className="h-64 rounded-2xl" />
        </div>
      </div>
    );
  }

  if (error || !dashboard) {
    return (
      <InlineError
        message={error ?? "Dashboard data was not returned by the backend."}
        onRetry={() => window.location.reload()}
      />
    );
  }

  const completionPercent = Math.min(dashboard.profileCompletionPercent, 100);
  const circumference = 2 * Math.PI * 40;
  const strokeDashoffset = circumference - (completionPercent / 100) * circumference;

  return (
    <div className="space-y-6">
      {/* Welcome header */}
      <section className="relative overflow-hidden rounded-2xl border border-border/60 bg-gradient-to-r from-primary/[0.04] via-card to-card p-6 shadow-sm">
        <div className="pointer-events-none absolute -right-16 -top-16 h-40 w-40 rounded-full bg-primary/5 blur-3xl" />
        <div className="relative flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
          <div>
            <div className="flex items-center gap-2">
              <Sparkles className="h-4 w-4 text-primary" />
              <p className="text-sm font-medium text-muted-foreground">Workspace</p>
            </div>
            <h1 className="mt-2 text-2xl font-bold tracking-tight">
              Welcome back, {user?.fullName?.split(" ")[0] ?? "User"}
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Here&apos;s what&apos;s happening with your projects and applications.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Badge tone={user?.isEmailVerified ? "success" : "warning"}>
              {user?.isEmailVerified ? "Email verified" : "Email not verified"}
            </Badge>
            <Badge tone="default">{primaryRole}</Badge>
          </div>
        </div>
      </section>

      {/* Metric cards */}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {metrics.map((metric, index) => {
          const Icon = metric.icon;
          const color = metricColors[index];
          return (
            <div
              key={metric.label}
              className="group rounded-2xl border border-border/60 bg-card p-5 shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-md"
            >
              <div className="flex items-center justify-between">
                <div className={`flex h-10 w-10 items-center justify-center rounded-xl ${color.bg} ${color.text} ring-1 ${color.ring}`}>
                  <Icon className="h-5 w-5" />
                </div>
                <TrendingUp className="h-4 w-4 text-muted-foreground/50" />
              </div>
              <p className="mt-4 text-3xl font-bold tracking-tight">{metric.value}</p>
              <p className="mt-1 text-sm font-medium text-foreground">{metric.label}</p>
              <p className="text-xs text-muted-foreground">{metric.hint}</p>
            </div>
          );
        })}
      </div>

      <div className="grid gap-4 lg:grid-cols-[1fr_360px]">
        {/* Profile completion with SVG ring */}
        <div className="rounded-2xl border border-border/60 bg-card shadow-sm">
          <div className="border-b border-border p-4">
            <h2 className="text-sm font-bold">Profile completion</h2>
          </div>
          <div className="flex items-center gap-6 p-6">
            <div className="relative flex h-24 w-24 shrink-0 items-center justify-center">
              <svg className="h-24 w-24 -rotate-90" viewBox="0 0 96 96">
                <circle cx="48" cy="48" r="40" fill="none" stroke="hsl(var(--muted))" strokeWidth="6" />
                <circle
                  cx="48"
                  cy="48"
                  r="40"
                  fill="none"
                  stroke="hsl(var(--primary))"
                  strokeWidth="6"
                  strokeLinecap="round"
                  strokeDasharray={circumference}
                  strokeDashoffset={strokeDashoffset}
                  className="transition-all duration-700"
                />
              </svg>
              <span className="absolute text-lg font-bold text-primary">{completionPercent}%</span>
            </div>
            <div className="min-w-0 flex-1">
              <p className="text-sm font-medium text-muted-foreground">
                Keep your profile complete to improve matching quality and attract the right collaborators.
              </p>
              <Link
                href="/profile"
                className="mt-3 inline-flex items-center gap-1.5 text-sm font-semibold text-primary hover:underline"
              >
                Update profile
                <ArrowUpRight className="h-3.5 w-3.5" />
              </Link>
            </div>
          </div>
        </div>

        {/* Quick links */}
        <div className="rounded-2xl border border-border/60 bg-card shadow-sm">
          <div className="border-b border-border p-4">
            <h2 className="text-sm font-bold">Quick links</h2>
          </div>
          <div className="space-y-1 p-3">
            {quickLinks.map((item) => {
              const Icon = item.icon;
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className="group flex items-center justify-between rounded-xl p-3 transition-colors hover:bg-accent"
                >
                  <span className="flex items-center gap-3">
                    <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-muted text-muted-foreground">
                      <Icon className="h-4 w-4" />
                    </span>
                    <span>
                      <span className="block text-sm font-medium">{item.title}</span>
                      <span className="block text-xs text-muted-foreground">{item.description}</span>
                    </span>
                  </span>
                  <ArrowUpRight className="h-4 w-4 text-muted-foreground/50 transition-transform group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
                </Link>
              );
            })}
          </div>
        </div>
      </div>

      <div className="grid gap-4 lg:grid-cols-[1fr_360px]">
        {/* Activity feed */}
        <div className="rounded-2xl border border-border/60 bg-card shadow-sm">
          <div className="flex items-center justify-between border-b border-border p-4">
            <h2 className="text-sm font-bold">Recent activity</h2>
            <Link href="/feed" className="text-xs font-medium text-primary hover:underline">
              View all
            </Link>
          </div>
          <div className="space-y-1 p-3">
            {feed.length === 0 ? (
              <div className="p-4">
                <EmptyState icon={Bell} title="No activity yet" description="Relevant project and application updates will appear here." />
              </div>
            ) : (
              feed.map((item) => (
                <div
                  key={item.id}
                  className="flex items-start justify-between gap-3 rounded-xl p-3 transition-colors hover:bg-muted/50"
                >
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="text-sm font-medium">{item.actorName ?? "System"}</p>
                      <StatusBadge value={item.type} />
                    </div>
                    <p className="mt-1 truncate text-sm text-muted-foreground">{item.title}</p>
                    {item.message ? <p className="mt-0.5 truncate text-xs text-muted-foreground">{item.message}</p> : null}
                  </div>
                  <p className="shrink-0 text-xs text-muted-foreground">
                    {dateFormatter.format(new Date(item.createdAt))}
                  </p>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Applications by status */}
        <div className="rounded-2xl border border-border/60 bg-card shadow-sm">
          <div className="border-b border-border p-4">
            <h2 className="text-sm font-bold">Applications by status</h2>
          </div>
          <div className="space-y-1 p-3">
            {dashboard.applicationsByStatus.length === 0 ? (
              <div className="p-4">
                <EmptyState icon={BriefcaseBusiness} title="No applications" description="Status breakdown will show after you apply." />
              </div>
            ) : (
              dashboard.applicationsByStatus.map((item) => (
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
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
