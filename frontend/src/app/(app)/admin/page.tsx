"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { useEffect, useState } from "react";
import { BarChart3, FileText, FolderKanban, Handshake, Settings, UsersRound } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { MockNotice } from "@/components/admin/mock-notice";
import { LoadingState } from "@/components/common/loading-state";
import { Panel, PanelBody } from "@/components/ui/panel";
import { SystemRoles } from "@/lib/constants";
import { mockService } from "@/services";
import type { AdminDashboardDto } from "@/types/admin";

const metrics = [
  { key: "totalUsers", label: "Users", icon: UsersRound },
  { key: "totalProjects", label: "Projects", icon: FolderKanban },
  { key: "pendingProjects", label: "Pending projects", icon: BarChart3 },
  { key: "publishedProjects", label: "Published projects", icon: BarChart3 },
  { key: "openReports", label: "Open reports", icon: FileText },
  { key: "totalInvestorInterests", label: "Investor interests", icon: Handshake },
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
      setDashboard(await mockService.getAdminDashboard());
    }

    void loadDashboard();
  }, []);

  if (!dashboard) return <LoadingState label="Loading admin dashboard" />;

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold">Admin Dashboard</h1>
        <p className="mt-2 text-sm text-muted-foreground">Mock-backed admin overview until backend admin endpoints are completed.</p>
      </div>
      <MockNotice label="Admin dashboard" />
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
