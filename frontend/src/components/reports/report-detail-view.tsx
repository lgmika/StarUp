import Link from "next/link";
import { ArrowLeft, Clock3 } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import type { ReportDetailDto } from "@/types/report";

export function ReportDetailView({ detail, backHref }: { detail: ReportDetailDto; backHref: string }) {
  const report = detail.report;
  return <div className="space-y-5"><Link href={backHref} className="inline-flex items-center gap-2 text-sm font-medium text-primary"><ArrowLeft className="h-4 w-4" />Back to reports</Link>
    <Panel><PanelHeader><PanelTitle>Report details</PanelTitle></PanelHeader><PanelBody className="space-y-4"><div className="flex flex-wrap gap-2"><Badge>{report.targetType}</Badge><Badge tone="warning">{report.status}</Badge>{report.reasonCode ? <Badge tone="muted">{report.reasonCode}</Badge> : null}</div><div><p className="text-sm font-medium">Description</p><p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-muted-foreground">{report.description ?? report.reason ?? "No description provided."}</p></div>{report.evidence ? <div><p className="text-sm font-medium">Evidence</p><p className="mt-2 whitespace-pre-wrap rounded-md bg-muted p-3 text-sm">{report.evidence}</p></div> : null}{report.resolution ? <div><p className="text-sm font-medium">Resolution</p><p className="mt-2 rounded-md bg-emerald-50 p-3 text-sm text-emerald-800">{report.resolution}</p></div> : null}<p className="text-xs text-muted-foreground">Created {new Date(report.createdAt).toLocaleString()}</p></PanelBody></Panel>
    <Panel><PanelHeader><PanelTitle>Action history</PanelTitle></PanelHeader><PanelBody className="space-y-3">{detail.actions.length ? detail.actions.map((action) => <div key={action.id} className="flex gap-3 rounded-md border border-border p-3"><Clock3 className="mt-0.5 h-4 w-4 text-muted-foreground" /><div><p className="text-sm font-medium">{action.action}</p><p className="mt-1 text-sm text-muted-foreground">{action.reason}</p><p className="mt-1 text-xs text-muted-foreground">{new Date(action.createdAt).toLocaleString()}</p></div></div>) : <p className="text-sm text-muted-foreground">No report actions yet.</p>}</PanelBody></Panel>
  </div>;
}
