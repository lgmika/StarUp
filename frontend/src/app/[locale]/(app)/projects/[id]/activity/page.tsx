"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Activity } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { ProjectWorkspaceNav } from "@/components/projects/project-workspace-nav";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { projectService } from "@/services/project-service";

export default function ProjectActivityPage() {
  const { id } = useParams<{ id: string }>();
  const query = useQuery({ queryKey: ["project-activity", id], queryFn: () => projectService.getProjectActivities(id) });
  if (query.isLoading) return <LoadingState label="Loading project activity" />;
  return <div className="space-y-5"><PageHeader title="Project activity" description="A chronological audit of visible project events." /><ProjectWorkspaceNav projectId={id} />
    {query.error ? <p className="rounded-md bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(query.error)}</p> : null}
    {!query.data?.items.length ? <EmptyState icon={Activity} title="No activity yet" description="Project events will appear here as work progresses." /> : <div className="space-y-3">{query.data.items.map((item) => <Panel key={item.id}><PanelBody><div className="flex flex-wrap items-center gap-2"><p className="font-medium">{item.title}</p><Badge tone="muted">{item.type}</Badge><Badge tone="muted">{item.visibility}</Badge></div>{item.message ? <p className="mt-2 text-sm text-muted-foreground">{item.message}</p> : null}<p className="mt-2 text-xs text-muted-foreground">{item.actorName ?? "System"} · {new Date(item.createdAt).toLocaleString()}</p></PanelBody></Panel>)}</div>}
  </div>;
}
