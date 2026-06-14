"use client";

import { useEffect, useState } from "react";
import { CheckCircle2, Search, UserCheck, XCircle } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { PageHeader } from "@/components/workspace/page-header";
import { StatusBadge } from "@/components/workspace/status-badge";
import { SystemRoles } from "@/lib/constants";
import { reportService } from "@/services/report-service";
import type { ReportDto } from "@/types/report";

export default function ModeratorReportsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Moderator, SystemRoles.Admin]}>
      <ModeratorReports />
    </RoleGuard>
  );
}

function ModeratorReports() {
  const [reports, setReports] = useState<ReportDto[]>([]);
  const [query, setQuery] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isMutating, setIsMutating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function loadReports() {
    setError(null);
    try {
      const response = await reportService.listModeratorReports();
      setReports(response.items);
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadReports();
  }, []);

  if (isLoading) return <LoadingState label="Đang tải reports" />;

  const visibleReports = reports.filter((report) => {
    const needle = query.toLowerCase();
    return `${report.targetType} ${report.targetId} ${report.reporterEmail ?? ""} ${report.description ?? ""} ${report.reasonCode ?? ""}`.toLowerCase().includes(needle);
  });

  async function runAction(action: () => Promise<unknown>) {
    setIsMutating(true);
    setError(null);
    try {
      await action();
      await loadReports();
    } catch (actionError) {
      setError(getApiErrorMessage(actionError));
    } finally {
      setIsMutating(false);
    }
  }

  return (
    <div className="space-y-5">
      <PageHeader title="Moderator Reports" description="Hàng chờ báo cáo vi phạm, phân công xử lý và action resolve/dismiss." />
      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}
      <Panel>
        <PanelBody>
          <div className="relative max-w-xl">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input className="pl-9" placeholder="Tìm theo target, reporter hoặc lý do..." value={query} onChange={(event) => setQuery(event.target.value)} />
          </div>
        </PanelBody>
      </Panel>
      <Panel>
        <PanelHeader>
          <PanelTitle>Reports queue</PanelTitle>
        </PanelHeader>
        <PanelBody className="overflow-x-auto">
          <table className="w-full min-w-[980px] text-left text-sm">
            <thead className="border-b border-border text-xs text-muted-foreground">
              <tr>
                <th className="py-2 font-medium">Target</th>
                <th className="py-2 font-medium">Reporter</th>
                <th className="py-2 font-medium">Reason</th>
                <th className="py-2 font-medium">Status</th>
                <th className="py-2 font-medium">Assigned</th>
                <th className="py-2 text-right font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {visibleReports.map((report) => (
                <tr key={report.id} className="border-b border-border align-top">
                  <td className="py-3">
                    <p className="font-medium">{report.targetType}</p>
                    <p className="text-xs text-muted-foreground">{report.targetType} · {report.createdAt}</p>
                  </td>
                  <td className="py-3 text-muted-foreground">{report.reporterEmail ?? report.reporterUserId}</td>
                  <td className="max-w-md py-3 text-muted-foreground">{report.description ?? report.reasonCode ?? report.reason}</td>
                  <td className="py-3"><StatusBadge value={report.status} /></td>
                  <td className="py-3 text-muted-foreground">{report.assignedModeratorId ?? "Unassigned"}</td>
                  <td className="py-3">
                    <div className="flex justify-end gap-2">
                      <Button variant="outline" size="icon" aria-label="Assign report" disabled={isMutating} onClick={() => void runAction(() => reportService.assign(report.id, "Assigned from moderator UI"))}>
                        <UserCheck className="h-4 w-4" />
                      </Button>
                      <Button variant="outline" size="icon" aria-label="Resolve report" disabled={isMutating} onClick={() => void runAction(() => reportService.resolve(report.id, "Resolved from moderator UI"))}>
                        <CheckCircle2 className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="icon" aria-label="Dismiss report" disabled={isMutating} onClick={() => void runAction(() => reportService.dismiss(report.id, "Dismissed from moderator UI"))}>
                        <XCircle className="h-4 w-4" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!error && visibleReports.length === 0 ? <p className="py-8 text-center text-sm text-muted-foreground">Không có report phù hợp.</p> : null}
        </PanelBody>
      </Panel>
    </div>
  );
}
