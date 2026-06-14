"use client";

import { useEffect, useState } from "react";
import { FileText } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { SystemRoles } from "@/lib/constants";
import { adminService } from "@/services";
import type { AdminAuditLogDto } from "@/types/admin";

export default function AdminAuditLogsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <AdminAuditLogs />
    </RoleGuard>
  );
}

function AdminAuditLogs() {
  const [logs, setLogs] = useState<AdminAuditLogDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function loadLogs() {
      const response = await adminService.listAuditLogs(1, 50);
      setLogs(response.items);
      setIsLoading(false);
    }

    void loadLogs();
  }, []);

  if (isLoading) return <LoadingState label="Loading audit logs" />;

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold">Audit Logs</h1>
        <p className="mt-2 text-sm text-muted-foreground">Backend audit trail for admin and moderation actions.</p>
      </div>
      {logs.length === 0 ? <EmptyState icon={FileText} title="No audit logs" description="The backend did not return any audit events yet." /> : null}
      <div className="grid gap-3">
        {logs.map((log) => (
          <Panel key={log.id}>
            <PanelBody>
              <div className="flex flex-wrap items-center gap-2">
                <p className="font-semibold">{log.action}</p>
                <Badge tone="muted">{log.resourceType}</Badge>
              </div>
              {log.reason ? <p className="mt-2 text-sm text-muted-foreground">{log.reason}</p> : null}
              <p className="mt-2 text-xs text-muted-foreground">Actor: {log.actorUserId ?? "system"} · {new Date(log.createdAt).toLocaleString()}</p>
            </PanelBody>
          </Panel>
        ))}
      </div>
    </div>
  );
}
