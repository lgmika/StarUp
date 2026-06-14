"use client";

import { Activity, Bell, Brain, Database, Lock, Settings } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { PageHeader } from "@/components/workspace/page-header";
import { SystemRoles } from "@/lib/constants";

const settings = [
  {
    id: "auth",
    group: "Security",
    name: "Authentication and role guards",
    value: "Configured in frontend and enforced by backend authorization policies.",
    status: "Active",
    icon: Lock,
  },
  {
    id: "notifications",
    group: "Engagement",
    name: "Notifications",
    value: "Connected to /notifications, unread count, read all, mark read, and archive endpoints.",
    status: "Active",
    icon: Bell,
  },
  {
    id: "ai",
    group: "AI",
    name: "Project AI helpers",
    value: "Create/edit project screens call AI suggestion and review endpoints where available.",
    status: "Active",
    icon: Brain,
  },
  {
    id: "background-jobs",
    group: "Operations",
    name: "Background jobs",
    value: "Admin can inspect executions and trigger maintenance through backend admin endpoints.",
    status: "Active",
    icon: Activity,
  },
  {
    id: "system-settings",
    group: "Configuration",
    name: "Editable system settings",
    value: "No backend settings endpoint is exposed yet, so this screen remains read-only.",
    status: "Pending endpoint",
    icon: Database,
  },
];

export default function AdminSettingsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <div className="space-y-5">
        <PageHeader
          title="Admin Settings"
          description="Read-only operational overview for configured frontend modules and backend-backed admin capabilities."
        />

        <div className="grid gap-4 lg:grid-cols-2">
          {settings.map((setting) => {
            const Icon = setting.icon;
            return (
              <Panel key={setting.id}>
                <PanelHeader className="flex flex-row items-start justify-between gap-3">
                  <div className="flex items-center gap-2">
                    <Icon className="h-4 w-4 text-muted-foreground" />
                    <PanelTitle>{setting.name}</PanelTitle>
                  </div>
                  <Badge tone={setting.status === "Active" ? "success" : "warning"}>{setting.status}</Badge>
                </PanelHeader>
                <PanelBody>
                  <p className="text-xs font-medium uppercase text-muted-foreground">{setting.group}</p>
                  <p className="mt-3 text-sm text-muted-foreground">{setting.value}</p>
                </PanelBody>
              </Panel>
            );
          })}
        </div>

        <Panel>
          <PanelBody className="flex items-start gap-3">
            <Settings className="mt-0.5 h-4 w-4 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">
              Editable quota, realtime, and platform policy controls should be added here once the backend exposes a dedicated settings API.
            </p>
          </PanelBody>
        </Panel>
      </div>
    </RoleGuard>
  );
}
