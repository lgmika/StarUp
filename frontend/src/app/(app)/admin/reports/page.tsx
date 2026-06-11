"use client";

import { useEffect, useState } from "react";
import { RoleGuard } from "@/components/auth/role-guard";
import { MockNotice } from "@/components/admin/mock-notice";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody } from "@/components/ui/panel";
import { REPORT_STATUS_LABELS, SystemRoles } from "@/lib/constants";
import { mockService } from "@/services";
import type { ReportDto } from "@/types/report";

export default function AdminReportsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <AdminReports />
    </RoleGuard>
  );
}

function AdminReports() {
  const [reports, setReports] = useState<ReportDto[]>([]);

  useEffect(() => {
    async function loadReports() {
      setReports(await mockService.getReports());
    }

    void loadReports();
  }, []);

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold">Admin Reports</h1>
        <p className="mt-2 text-sm text-muted-foreground">Report moderation surface prepared with clearly marked mock data.</p>
      </div>
      <MockNotice label="Admin reports" />
      <div className="grid gap-3">
        {reports.map((report) => (
          <Panel key={report.id}>
            <PanelBody>
              <div className="flex flex-wrap items-center gap-2">
                <p className="font-semibold">{report.targetType}</p>
                <Badge tone="warning">{REPORT_STATUS_LABELS[report.status]}</Badge>
              </div>
              <p className="mt-2 text-sm text-muted-foreground">{report.reason}</p>
              <p className="mt-2 text-xs text-muted-foreground">Target: {report.targetId} · Reporter: {report.reporterUserId}</p>
            </PanelBody>
          </Panel>
        ))}
      </div>
    </div>
  );
}
