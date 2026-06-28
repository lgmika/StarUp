"use client";

import { useQuery } from "@tanstack/react-query";
import { FileSignature, ShieldCheck } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { queryKeys } from "@/lib/query-keys";
import { PageHeader } from "@/components/workspace/page-header";
import { StatusBadge } from "@/components/workspace/status-badge";
import { ndaService } from "@/services/nda-service";

export default function NdaAgreementsPage() {
  const agreementsQuery = useQuery({ queryKey: [...queryKeys.nda, "agreements"], queryFn: ndaService.listMyAgreements });
  const agreements = agreementsQuery.data ?? [];

  if (agreementsQuery.isLoading) return <LoadingState label="Loading NDA agreements" />;

  return <div className="space-y-5">
    <PageHeader title="NDA Agreements" description="Review the NDA versions you have accepted for protected projects." />
    {agreementsQuery.error ? <div className="rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(agreementsQuery.error)}</div> : null}
    <div className="grid gap-4 md:grid-cols-3"><Metric label="Accepted" value={agreements.length} /><Metric label="Pending" value={0} /><Metric label="Rejected" value={0} /></div>
    <Panel><PanelHeader><PanelTitle>Agreement history</PanelTitle></PanelHeader><PanelBody className="space-y-3">
      {agreements.map((agreement) => <article key={agreement.id} className="flex flex-col justify-between gap-3 rounded-md border border-border p-4 md:flex-row md:items-center"><div><div className="flex flex-wrap items-center gap-2"><ShieldCheck className="h-4 w-4 text-muted-foreground" /><p className="font-medium">Project {agreement.projectId}</p><StatusBadge value="Accepted" /></div><p className="mt-2 text-sm text-muted-foreground">Version {agreement.versionNumber} / Accepted {new Date(agreement.acceptedAt).toLocaleString()}</p></div><div className="flex items-center gap-2 text-xs text-muted-foreground"><FileSignature className="h-4 w-4" />Agreement {agreement.id}</div></article>)}
      {!agreementsQuery.error && agreements.length === 0 ? <p className="py-8 text-center text-sm text-muted-foreground">No NDA agreements yet.</p> : null}
    </PanelBody></Panel>
  </div>;
}

function Metric({ label, value }: { label: string; value: number }) {
  return <Panel><PanelBody><p className="text-2xl font-semibold">{value}</p><p className="mt-1 text-sm text-muted-foreground">{label}</p></PanelBody></Panel>;
}
