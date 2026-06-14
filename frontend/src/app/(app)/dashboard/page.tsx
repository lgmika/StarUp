"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { ArrowUpRight, Bell, BriefcaseBusiness, CalendarClock, FileText, Folders, Heart, ShieldCheck, UserRoundCheck } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { StatusBadge } from "@/components/workspace/status-badge";
import { getPrimaryRole } from "@/lib/permissions";
import { activityService, dashboardService } from "@/services";
import { useAuthStore } from "@/stores/auth-store";
import type { ActivityDto } from "@/types/activity";
import type { UserDashboardDto } from "@/types/dashboard";

const quickLinks = [
  { title: "Complete profile", href: "/profile", icon: FileText },
  { title: "My owned projects", href: "/projects/me/owned", icon: Folders },
  { title: "Applications", href: "/applications", icon: ShieldCheck },
  { title: "Notifications", href: "/notifications", icon: Bell },
];

const dateFormatter = new Intl.DateTimeFormat("vi-VN", {
  dateStyle: "medium",
  timeStyle: "short",
});

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
      { label: "Applications", value: dashboard.applications, hint: "Total applications in the selected window", icon: BriefcaseBusiness },
      { label: "Upcoming interviews", value: dashboard.upcomingInterviews, hint: "Interviews that need attention", icon: CalendarClock },
      { label: "Joined projects", value: dashboard.joinedProjects, hint: "Projects where you are a member", icon: Folders },
      { label: "Saved projects", value: dashboard.savedProjects, hint: "Projects saved for later review", icon: Heart },
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

  if (isLoading) return <LoadingState label="Loading dashboard" />;

  if (error || !dashboard) {
    return (
      <Panel>
        <PanelBody className="flex flex-col items-start gap-3">
          <Badge tone="danger">Dashboard unavailable</Badge>
          <p className="text-sm text-muted-foreground">{error ?? "Dashboard data was not returned by the backend."}</p>
          <Button onClick={() => window.location.reload()}>Retry</Button>
        </PanelBody>
      </Panel>
    );
  }

  return (
    <div className="space-y-6">
      <section className="flex flex-col justify-between gap-4 rounded-md border border-border bg-card p-5 shadow-sm sm:flex-row sm:items-center">
        <div>
          <p className="text-sm text-muted-foreground">Workspace</p>
          <h1 className="mt-1 text-2xl font-semibold">{user?.fullName ?? "StartupConnect"}</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Dashboard summary for applications, projects, profile completion, and recent activity.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Badge tone={user?.isEmailVerified ? "success" : "warning"}>
            {user?.isEmailVerified ? "Email verified" : "Email not verified"}
          </Badge>
          <Badge tone="default">{primaryRole}</Badge>
        </div>
      </section>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {metrics.map((metric) => {
          const Icon = metric.icon;
          return (
            <Panel key={metric.label}>
              <PanelBody>
                <Icon className="h-5 w-5 text-muted-foreground" />
                <p className="mt-4 text-2xl font-semibold">{metric.value}</p>
                <p className="mt-1 text-sm font-medium">{metric.label}</p>
                <p className="mt-1 text-xs text-muted-foreground">{metric.hint}</p>
              </PanelBody>
            </Panel>
          );
        })}
      </div>

      <div className="grid gap-4 lg:grid-cols-[1fr_360px]">
        <Panel>
          <PanelHeader>
            <PanelTitle>Profile completion</PanelTitle>
          </PanelHeader>
          <PanelBody>
            <div className="flex items-center justify-between gap-3">
              <div>
                <p className="text-3xl font-semibold">{dashboard.profileCompletionPercent}%</p>
                <p className="mt-1 text-sm text-muted-foreground">Keep your profile complete to improve matching quality.</p>
              </div>
              <UserRoundCheck className="h-10 w-10 text-muted-foreground" />
            </div>
            <div className="mt-5 h-2 rounded-full bg-muted">
              <div className="h-full rounded-full bg-primary" style={{ width: `${Math.min(dashboard.profileCompletionPercent, 100)}%` }} />
            </div>
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader>
            <PanelTitle>Quick links</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-2">
            {quickLinks.map((item) => {
              const Icon = item.icon;
              return (
                <Link key={item.href} href={item.href} className="flex items-center justify-between rounded-md border border-border p-3 hover:bg-accent">
                  <span className="flex items-center gap-3 text-sm font-medium">
                    <Icon className="h-4 w-4 text-muted-foreground" />
                    {item.title}
                  </span>
                  <ArrowUpRight className="h-4 w-4 text-muted-foreground" />
                </Link>
              );
            })}
          </PanelBody>
        </Panel>
      </div>

      <div className="grid gap-4 lg:grid-cols-[1fr_360px]">
        <Panel>
          <PanelHeader>
            <PanelTitle>Recent activity</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-3">
            {feed.length === 0 ? (
              <EmptyState icon={Bell} title="No activity yet" description="Relevant project and application updates will appear here." />
            ) : (
              feed.map((item) => (
                <div key={item.id} className="flex items-start justify-between gap-3 rounded-md border border-border p-3">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="text-sm font-medium">{item.actorName ?? "System"}</p>
                      <StatusBadge value={item.type} />
                    </div>
                    <p className="mt-1 text-sm text-muted-foreground">{item.title}</p>
                    {item.message ? <p className="mt-1 text-xs text-muted-foreground">{item.message}</p> : null}
                  </div>
                  <p className="shrink-0 text-xs text-muted-foreground">{dateFormatter.format(new Date(item.createdAt))}</p>
                </div>
              ))
            )}
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader>
            <PanelTitle>Applications by status</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-3">
            {dashboard.applicationsByStatus.length === 0 ? (
              <EmptyState icon={BriefcaseBusiness} title="No applications" description="Application status breakdown will show up after you apply to projects." />
            ) : (
              dashboard.applicationsByStatus.map((item) => (
                <div key={item.status} className="flex items-center justify-between rounded-md border border-border p-3">
                  <StatusBadge value={item.status} />
                  <span className="text-sm font-semibold">{item.count}</span>
                </div>
              ))
            )}
          </PanelBody>
        </Panel>
      </div>
    </div>
  );
}
