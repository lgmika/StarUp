"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { FileCheck2 } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { ProjectWorkspaceNav } from "@/components/projects/project-workspace-nav";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { ndaService } from "@/services/nda-service";

export default function ProjectNdaAgreementsPage() {
  const { id } = useParams<{ id: string }>();
  const query = useQuery({ queryKey: ["project-nda-agreements", id], queryFn: () => ndaService.listProjectAgreements(id) });
  if (query.isLoading) return <LoadingState label="Loading NDA records" />;
  return <div className="space-y-5"><PageHeader title="NDA records" description="Immutable acceptance records for protected project access." /><ProjectWorkspaceNav projectId={id} />
    {query.error ? <p className="rounded-md bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(query.error)}</p> : null}
    {!query.data?.length ? <EmptyState icon={FileCheck2} title="No NDA agreements" description="Accepted NDA records will appear here." /> : <div className="space-y-3">{query.data.map((agreement) => <Panel key={agreement.id}><PanelBody className="flex flex-col justify-between gap-3 md:flex-row md:items-center"><div><div className="flex items-center gap-2"><p className="font-medium">User {agreement.userId}</p><Badge tone="success">Accepted</Badge><Badge tone="muted">Version {agreement.versionNumber}</Badge></div><p className="mt-2 text-xs text-muted-foreground">Template {agreement.templateId}</p></div><time className="text-sm text-muted-foreground">{new Date(agreement.acceptedAt).toLocaleString()}</time></PanelBody></Panel>)}</div>}
  </div>;
}
