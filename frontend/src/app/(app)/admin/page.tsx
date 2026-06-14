"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { useEffect, useState } from "react";
import { BarChart3, Bot, FileText, FolderKanban, HardDrive, Settings, UsersRound, UserCheck } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Panel, PanelBody } from "@/components/ui/panel";
import { SystemRoles } from "@/lib/constants";
import { adminService } from "@/services";
import type { AdminDashboardDto } from "@/types/admin";

const metrics = [
  { key: "totalUsers", label: "Users", icon: UsersRound },
  { key: "activeUsers", label: "Active users", icon: UserCheck },
  { key: "verifiedUsers", label: "Verified users", icon: UserCheck },
  { key: "totalProjects", label: "Projects", icon: FolderKanban },
  { key: "pendingModeration", label: "Pending moderation", icon: BarChart3 },
  { key: "openReports", label: "Open reports", icon: FileText },
  { key: "applications", label: "Applications", icon: FileText },
  { key: "investors", label: "Investors", icon: UsersRound },
  { key: "aiRequests", label: "AI requests", icon: Bot },
  { key: "storageBytes", label: "Storage bytes", icon: HardDrive },
] as const;

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

  if (!dashboard) return <LoadingState label="Loading admin dashboard" />;

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold">Admin Dashboard</h1>
        <p className="mt-2 text-sm text-muted-foreground">System health, moderation workload, and platform activity from backend admin endpoints.</p>
      </div>
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {metrics.map((metric) => {
          const Icon = metric.icon;
          return (
            <Panel key={metric.key}>
              <PanelBody>
                <Icon className="h-5 w-5 text-muted-foreground" />
                <p className="mt-4 text-2xl font-semibold">{dashboard[metric.key]}</p>
                <p className="mt-1 text-sm text-muted-foreground">{metric.label}</p>
              </PanelBody>
            </Panel>
          );
        })}
      </div>
      <Panel>
        <PanelBody className="flex flex-wrap gap-2">
          <AdminLink href="/admin/users" label="Users" />
          <AdminLink href="/admin/reports" label="Reports" />
          <AdminLink href="/admin/audit-logs" label="Audit logs" />
          <AdminLink href="/admin/nda-templates" label="NDA templates" />
          <AdminLink href="/admin/settings" label="Settings" icon={<Settings className="h-4 w-4" />} />
        </PanelBody>
      </Panel>
    </div>
  );
}

function AdminLink({ href, label, icon }: { href: string; label: string; icon?: ReactNode }) {
  return (
    <Link className="inline-flex h-10 items-center gap-2 rounded-md border border-border px-4 text-sm font-medium hover:bg-accent" href={href}>
      {icon}
      {label}
    </Link>
  );
}
