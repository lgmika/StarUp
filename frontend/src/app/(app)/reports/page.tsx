"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { FileText, RefreshCw } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { LoadingState } from "@/components/common/loading-state";
import { getApiErrorMessage } from "@/lib/api";
import { REPORT_REASON_LABELS, REPORT_STATUS_LABELS } from "@/lib/constants";
import { queryKeys } from "@/lib/query-keys";
import { reportService } from "@/services";
import { ReportStatus } from "@/types/enums";

export default function MyReportsPage() {
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const reportsQuery = useQuery({ queryKey: [...queryKeys.reports, { status, page }], queryFn: () => reportService.listMyReports({ status: status as ReportStatus || undefined, page, pageSize: 20 }) });
  if (reportsQuery.isLoading) return <LoadingState label="Loading reports" />;
  const result = reportsQuery.data;
  const pages = result ? Math.max(1, Math.ceil(result.total / result.pageSize)) : 1;

  return <div className="space-y-5">
    <div className="flex flex-col justify-between gap-4 md:flex-row md:items-end"><div><h1 className="text-2xl font-semibold">My Reports</h1><p className="mt-2 text-sm text-muted-foreground">Track reports submitted from project, application, message, user, or portfolio context.</p></div><div className="flex gap-2"><select className="h-10 rounded-md border border-input bg-background px-3 text-sm" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1); }}><option value="">All statuses</option>{Object.values(ReportStatus).map((item) => <option key={item} value={item}>{REPORT_STATUS_LABELS[item]}</option>)}</select><Button variant="outline" onClick={() => void reportsQuery.refetch()}><RefreshCw className="h-4 w-4" />Refresh</Button></div></div>
    <div className="rounded-md border border-border bg-muted/40 p-4 text-sm text-muted-foreground">To submit a new report, open the relevant project or other supported item and choose <span className="font-medium text-foreground">Report</span>. The backend validates the target before submission.</div>
    {reportsQuery.error ? <p className="rounded-md bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(reportsQuery.error)}</p> : null}
    {!result?.items.length ? <EmptyState icon={FileText} title="No reports" description="Reports submitted from supported content will appear here." /> : <div className="grid gap-3">{result.items.map((report) => <Panel key={report.id}><PanelBody><div className="flex flex-wrap items-center gap-2"><p className="font-semibold">{report.targetType}</p><Badge tone={report.status === ReportStatus.Resolved ? "success" : report.status === ReportStatus.Dismissed ? "muted" : "warning"}>{REPORT_STATUS_LABELS[report.status]}</Badge>{report.reasonCode ? <Badge tone="muted">{REPORT_REASON_LABELS[report.reasonCode]}</Badge> : null}</div><p className="mt-2 text-sm text-muted-foreground">{report.description ?? report.reason ?? "No description provided."}</p>{report.resolution ? <p className="mt-2 rounded-md bg-muted p-3 text-sm">Resolution: {report.resolution}</p> : null}<p className="mt-2 text-xs text-muted-foreground">Created {new Date(report.createdAt).toLocaleString()}</p></PanelBody></Panel>)}</div>}
    <div className="flex items-center justify-between"><p className="text-xs text-muted-foreground">Page {page} of {pages}</p><div className="flex gap-2"><Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Previous</Button><Button size="sm" variant="outline" disabled={page >= pages} onClick={() => setPage((value) => value + 1)}>Next</Button></div></div>
  </div>;
}
