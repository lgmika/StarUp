"use client";

import { useEffect, useState } from "react";
import { FileText } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { REPORT_STATUS_LABELS, SystemRoles } from "@/lib/constants";
import { reportService } from "@/services";
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
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function loadReports() {
      const response = await reportService.listModeratorReports({ pageSize: 50 });
      setReports(response.items);
      setIsLoading(false);
    }

    void loadReports();
  }, []);

  if (isLoading) return <LoadingState label="Loading reports" />;

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold">Admin Reports</h1>
        <p className="mt-2 text-sm text-muted-foreground">Review platform reports from the backend moderation queue.</p>
      </div>

      {reports.length === 0 ? <EmptyState icon={FileText} title="No reports" description="There are no reports in the moderation queue right now." /> : null}

      <div className="grid gap-3">
        {reports.map((report) => (
          <Panel key={report.id}>
            <PanelBody>
              <div className="flex flex-wrap items-center gap-2">
                <p className="font-semibold">{report.targetType}</p>
                <Badge tone="warning">{REPORT_STATUS_LABELS[report.status]}</Badge>
                {report.reasonCode ? <Badge tone="muted">{report.reasonCode}</Badge> : null}
              </div>
              <p className="mt-2 text-sm text-muted-foreground">{report.reason ?? report.description ?? "No report reason provided."}</p>
              <p className="mt-2 text-xs text-muted-foreground">
                Target: {report.targetId} · Reporter: {report.reporterEmail ?? report.reporterUserId}
              </p>
            </PanelBody>
          </Panel>
        ))}
      </div>
    </div>
  );
}
