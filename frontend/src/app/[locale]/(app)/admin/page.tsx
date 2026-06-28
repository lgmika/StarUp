"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  ArrowUpRight,
  BarChart3,
  Bot,
  FileText,
  FolderKanban,
  HardDrive,
  Mail,
  ScrollText,
  Settings,
  Shield,
  Sparkles,
  TrendingUp,
  UserCheck,
  UsersRound,
} from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { Skeleton } from "@/components/ui/skeleton";
import { SystemRoles } from "@/lib/constants";
import { adminService } from "@/services";
import type { AdminDashboardDto } from "@/types/admin";

const metricColors = [
  { bg: "bg-blue-50", text: "text-blue-600", ring: "ring-blue-100" },
  { bg: "bg-emerald-50", text: "text-emerald-600", ring: "ring-emerald-100" },
  { bg: "bg-teal-50", text: "text-teal-600", ring: "ring-teal-100" },
  { bg: "bg-violet-50", text: "text-violet-600", ring: "ring-violet-100" },
  { bg: "bg-amber-50", text: "text-amber-600", ring: "ring-amber-100" },
  { bg: "bg-rose-50", text: "text-rose-600", ring: "ring-rose-100" },
  { bg: "bg-indigo-50", text: "text-indigo-600", ring: "ring-indigo-100" },
  { bg: "bg-purple-50", text: "text-purple-600", ring: "ring-purple-100" },
  { bg: "bg-cyan-50", text: "text-cyan-600", ring: "ring-cyan-100" },
  { bg: "bg-orange-50", text: "text-orange-600", ring: "ring-orange-100" },
];

const metrics = [
  { key: "totalUsers", label: "Total users", icon: UsersRound },
  { key: "activeUsers", label: "Active users", icon: UserCheck },
  { key: "verifiedUsers", label: "Verified users", icon: UserCheck },
  { key: "totalProjects", label: "Projects", icon: FolderKanban },
  { key: "pendingModeration", label: "Pending moderation", icon: BarChart3 },
  { key: "openReports", label: "Open reports", icon: FileText },
  { key: "applications", label: "Applications", icon: FileText },
  { key: "investors", label: "Investors", icon: UsersRound },
  { key: "aiRequests", label: "AI requests", icon: Bot },
  { key: "storageBytes", label: "Storage (bytes)", icon: HardDrive },
] as const;

const adminLinks = [
  { href: "/admin/users", label: "Users", description: "Manage accounts & roles", icon: UsersRound },
  { href: "/admin/projects", label: "Projects", description: "View & manage projects", icon: FolderKanban },
  { href: "/admin/reports", label: "Reports", description: "Review user reports", icon: Shield },
  { href: "/admin/nda-templates", label: "NDA Templates", description: "Manage NDA templates", icon: ScrollText },
  { href: "/admin/subscriptions", label: "Subscriptions", description: "Plans & quotas", icon: Sparkles },
  { href: "/admin/background-jobs", label: "Background Jobs", description: "Monitor & run jobs", icon: Bot },
  { href: "/admin/email-outbox", label: "Email Outbox", description: "Track sent emails", icon: Mail },
  { href: "/admin/settings", label: "Settings", description: "Platform configuration", icon: Settings },
];

export default function AdminPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <AdminDashboard />
    </RoleGuard>
  );
}

function AdminDashboard() {
  const [dashboard, setDashboard] = useState<AdminDashboardDto | null>(null);

  useEffect(() => {
    async function loadDashboard() {
      setDashboard(await adminService.getDashboard());
    }

    void loadDashboard();
  }, []);

  if (!dashboard) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-24 w-full rounded-2xl" />
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
          {Array.from({ length: 10 }).map((_, i) => (
            <Skeleton key={i} className="h-28 rounded-2xl" />
          ))}
        </div>
        <Skeleton className="h-40 rounded-2xl" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <section className="relative overflow-hidden rounded-2xl border border-border/60 bg-gradient-to-r from-violet-500/[0.04] via-card to-card p-6 shadow-sm">
        <div className="pointer-events-none absolute -right-16 -top-16 h-40 w-40 rounded-full bg-violet-500/5 blur-3xl" />
        <div className="relative">
          <div className="flex items-center gap-2">
            <Shield className="h-4 w-4 text-violet-600" />
            <p className="text-sm font-medium text-muted-foreground">Admin Console</p>
          </div>
          <h1 className="mt-2 text-2xl font-bold tracking-tight">
            Platform Overview
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            System health, moderation workload, and platform activity.
          </p>
        </div>
      </section>

      {/* Metric cards */}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        {metrics.map((metric, index) => {
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
              <p className="mt-3 text-2xl font-bold tracking-tight">{dashboard[metric.key]}</p>
              <p className="mt-0.5 text-xs font-medium text-muted-foreground">{metric.label}</p>
            </div>
          );
        })}
      </div>

      {/* Quick navigation */}
      <div className="rounded-2xl border border-border/60 bg-card shadow-sm">
        <div className="border-b border-border p-4">
          <h2 className="text-sm font-bold">Quick Navigation</h2>
        </div>
        <div className="grid gap-1 p-3 sm:grid-cols-2 lg:grid-cols-4">
          {adminLinks.map((item) => {
            const Icon = item.icon;
            return (
              <Link
                key={item.href}
                href={item.href}
                className="group flex items-center gap-3 rounded-xl p-3 transition-colors hover:bg-accent"
              >
                <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground">
                  <Icon className="h-4 w-4" />
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block text-sm font-medium">{item.label}</span>
                  <span className="block truncate text-xs text-muted-foreground">{item.description}</span>
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
