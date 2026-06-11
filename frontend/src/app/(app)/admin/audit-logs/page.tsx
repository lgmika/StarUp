"use client";

import { useEffect, useState } from "react";
import { RoleGuard } from "@/components/auth/role-guard";
import { MockNotice } from "@/components/admin/mock-notice";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody } from "@/components/ui/panel";
import { SystemRoles } from "@/lib/constants";
import { mockService } from "@/services";
import type { AuditLogDto } from "@/types/admin";

export default function AdminAuditLogsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <AdminAuditLogs />
    </RoleGuard>
  );
}

function AdminAuditLogs() {
  const [logs, setLogs] = useState<AuditLogDto[]>([]);

  useEffect(() => {
    async function loadLogs() {
      setLogs(await mockService.getAuditLogs());
    }

    void loadLogs();
  }, []);

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold">Audit Logs</h1>
        <p className="mt-2 text-sm text-muted-foreground">Audit log UI remains mock-backed until public audit endpoints exist.</p>
      </div>
      <MockNotice label="Audit logs" />
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
