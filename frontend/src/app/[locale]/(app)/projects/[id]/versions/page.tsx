"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { FileClock } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { ProjectWorkspaceNav } from "@/components/projects/project-workspace-nav";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { projectService } from "@/services/project-service";

export default function ProjectVersionsPage() {
  const { id } = useParams<{ id: string }>();
  const query = useQuery({ queryKey: ["project-versions", id], queryFn: () => projectService.getProjectVersions(id) });
  if (query.isLoading) return <LoadingState label="Loading project versions" />;
  return <div className="space-y-5"><PageHeader title="Version history" description="Snapshots created when the project definition changes." /><ProjectWorkspaceNav projectId={id} />
    {query.error ? <p className="rounded-md bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(query.error)}</p> : null}
    {!query.data?.length ? <EmptyState icon={FileClock} title="No versions" description="Saved project changes will create version records." /> : <div className="space-y-3">{query.data.map((version) => <Panel key={version.id}><PanelBody className="flex items-start justify-between gap-4"><div><div className="flex items-center gap-2"><p className="font-medium">Version {version.versionNumber}</p><Badge tone="muted">Snapshot</Badge></div><p className="mt-2 text-sm text-muted-foreground">{version.changeReason || "Project updated"}</p></div><time className="shrink-0 text-xs text-muted-foreground">{new Date(version.createdAt).toLocaleString()}</time></PanelBody></Panel>)}</div>}
  </div>;
}
