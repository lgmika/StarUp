"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { MapPin, Sparkles } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { ProjectWorkspaceNav } from "@/components/projects/project-workspace-nav";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { projectService } from "@/services/project-service";

export default function RecommendedMembersPage() {
  const { id } = useParams<{ id: string }>();
  const query = useQuery({ queryKey: ["project-recommended-members", id], queryFn: () => projectService.getRecommendedMembers(id) });
  if (query.isLoading) return <LoadingState label="Finding recommended members" />;
  return <div className="space-y-5"><PageHeader title="Recommended teammates" description="Backend-ranked candidates based on project requirements, skills, and profile fit." /><ProjectWorkspaceNav projectId={id} />
    {query.error ? <p className="rounded-md bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(query.error)}</p> : null}
    {!query.data?.items.length ? <EmptyState icon={Sparkles} title="No recommendations yet" description="Add required skills and open roles to improve matching." /> : <div className="grid gap-3 lg:grid-cols-2">{query.data.items.map((member) => <Panel key={member.recommendationId}><PanelBody><div className="flex items-start justify-between gap-3"><div><Link href={`/members/${member.userId}`} className="font-semibold hover:text-primary">{member.fullName}</Link><p className="mt-1 text-sm text-muted-foreground">{member.headline || "StartupConnect member"}</p>{member.location ? <p className="mt-2 flex items-center gap-1 text-xs text-muted-foreground"><MapPin className="h-3.5 w-3.5" />{member.location}</p> : null}</div><div className="rounded-md bg-primary/10 px-3 py-2 text-center text-primary"><p className="text-lg font-semibold">{Math.round(member.score)}</p><p className="text-[10px] uppercase">Match</p></div></div><div className="mt-4 flex flex-wrap gap-2">{member.matchedSkills.map((skill) => <Badge key={skill} tone="success">{skill}</Badge>)}</div><div className="mt-4 space-y-2 border-t border-border pt-3">{member.breakdown.slice(0, 3).map((item) => <div key={item.key} className="flex items-start justify-between gap-3 text-xs"><span className="text-muted-foreground">{item.explanation}</span><span className="font-medium text-primary">+{item.points}</span></div>)}</div></PanelBody></Panel>)}</div>}
  </div>;
}
