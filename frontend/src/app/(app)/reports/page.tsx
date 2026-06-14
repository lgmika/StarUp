"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { FileText, Loader2, Plus, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { getApiErrorMessage } from "@/lib/api";
import { REPORT_REASON_LABELS, REPORT_STATUS_LABELS } from "@/lib/constants";
import { reportService } from "@/services";
import { ReportReasonCode, ReportStatus } from "@/types/enums";
import type { ReportDto } from "@/types/report";

const targetTypes = ["Project", "User", "Application", "Message", "Portfolio"];

const defaultForm = {
  targetType: "Project",
  targetId: "",
  reasonCode: ReportReasonCode.Other,
  description: "",
  evidence: "",
};

export default function MyReportsPage() {
  const [reports, setReports] = useState<ReportDto[]>([]);
  const [status, setStatus] = useState("");
  const [form, setForm] = useState(defaultForm);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const counts = useMemo(() => {
    return reports.reduce<Record<string, number>>((acc, report) => {
      acc[report.status] = (acc[report.status] ?? 0) + 1;
      return acc;
    }, {});
  }, [reports]);

  async function loadReports(nextStatus = status) {
    try {
      setError(null);
      setIsLoading(true);
      const response = await reportService.listMyReports({
        status: nextStatus ? (nextStatus as ReportStatus) : undefined,
        pageSize: 50,
      });
      setReports(response.items);
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
      setReports([]);
    } finally {
      setIsLoading(false);
    }
  }

  async function submitReport(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!form.targetId.trim() || !form.description.trim()) {
      toast.error("Target ID and description are required.");
      return;
    }

    try {
      setIsSubmitting(true);
      await reportService.create({
        targetType: form.targetType,
        targetId: form.targetId.trim(),
        reasonCode: form.reasonCode,
        description: form.description.trim(),
        evidence: form.evidence.trim() || undefined,
      });
      setForm(defaultForm);
      toast.success("Report submitted.");
      await loadReports();
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    } finally {
      setIsSubmitting(false);
    }
  }

  useEffect(() => {
    void loadReports("");
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="grid gap-6 xl:grid-cols-[420px_1fr]">
      <section className="space-y-5">
        <div>
          <h1 className="text-2xl font-semibold">My Reports</h1>
          <p className="mt-2 text-sm text-muted-foreground">Submit and track reports through backend report endpoints.</p>
        </div>

        <div className="grid gap-3 sm:grid-cols-3 xl:grid-cols-1">
          <Metric label="Total" value={reports.length} />
          <Metric label="Pending" value={counts.Pending ?? 0} />
          <Metric label="Investigating" value={counts.Investigating ?? 0} />
        </div>

        <Panel>
          <PanelHeader>
            <PanelTitle>Submit report</PanelTitle>
          </PanelHeader>
          <PanelBody>
            <form className="space-y-4" onSubmit={submitReport}>
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Target type</span>
                <select
                  className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  value={form.targetType}
                  onChange={(event) => setForm((value) => ({ ...value, targetType: event.target.value }))}
                >
                  {targetTypes.map((targetType) => <option key={targetType} value={targetType}>{targetType}</option>)}
                </select>
              </label>
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Target ID</span>
                <Input value={form.targetId} onChange={(event) => setForm((value) => ({ ...value, targetId: event.target.value }))} />
              </label>
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Reason</span>
                <select
                  className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  value={form.reasonCode}
                  onChange={(event) => setForm((value) => ({ ...value, reasonCode: event.target.value as ReportReasonCode }))}
                >
                  {Object.values(ReportReasonCode).map((reason) => (
                    <option key={reason} value={reason}>{REPORT_REASON_LABELS[reason]}</option>
                  ))}
                </select>
              </label>
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Description</span>
                <textarea
                  className="min-h-24 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  value={form.description}
                  onChange={(event) => setForm((value) => ({ ...value, description: event.target.value }))}
                />
              </label>
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Evidence</span>
                <Input value={form.evidence} onChange={(event) => setForm((value) => ({ ...value, evidence: event.target.value }))} />
              </label>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
                Submit report
              </Button>
            </form>
          </PanelBody>
        </Panel>
      </section>

      <section className="space-y-5">
        <div className="flex flex-col justify-between gap-3 md:flex-row md:items-end">
          <div>
            <h2 className="text-lg font-semibold">Report history</h2>
            <p className="mt-1 text-sm text-muted-foreground">Filter and review the reports you submitted.</p>
          </div>
          <div className="flex gap-2">
            <select
              className="h-10 rounded-md border border-input bg-background px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
              value={status}
              onChange={(event) => {
                setStatus(event.target.value);
                void loadReports(event.target.value);
              }}
            >
              <option value="">All statuses</option>
              {Object.values(ReportStatus).map((item) => (
                <option key={item} value={item}>{REPORT_STATUS_LABELS[item]}</option>
              ))}
            </select>
            <Button variant="outline" onClick={() => void loadReports()} disabled={isLoading}>
              <RefreshCw className="h-4 w-4" />
              Refresh
            </Button>
          </div>
        </div>

        {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}

        {isLoading ? (
          <LoadingState label="Loading reports" />
        ) : reports.length === 0 ? (
          <EmptyState icon={FileText} title="No reports" description="Submitted reports will appear here with moderation status." />
        ) : (
          <div className="grid gap-3">
            {reports.map((report) => (
              <Panel key={report.id}>
                <PanelBody>
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="font-semibold">{report.targetType}</p>
                    <Badge tone={report.status === ReportStatus.Resolved ? "success" : report.status === ReportStatus.Dismissed ? "muted" : "warning"}>
                      {REPORT_STATUS_LABELS[report.status]}
                    </Badge>
                    {report.reasonCode ? <Badge tone="muted">{REPORT_REASON_LABELS[report.reasonCode]}</Badge> : null}
                  </div>
                  <p className="mt-2 text-sm text-muted-foreground">{report.description ?? report.reason ?? "No description provided."}</p>
                  {report.resolution ? <p className="mt-2 text-sm text-muted-foreground">Resolution: {report.resolution}</p> : null}
                  <p className="mt-2 text-xs text-muted-foreground">
                    Target: {report.targetId} · Created {new Date(report.createdAt).toLocaleString()}
                  </p>
                </PanelBody>
              </Panel>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <Panel>
      <PanelBody>
        <p className="text-2xl font-semibold">{value}</p>
        <p className="mt-1 text-sm text-muted-foreground">{label}</p>
      </PanelBody>
    </Panel>
  );
}
