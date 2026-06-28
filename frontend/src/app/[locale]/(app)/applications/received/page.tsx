"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, ClipboardList, ListChecks, MessageSquareText, X } from "lucide-react";
import { toast } from "sonner";
import { ApplicationStatusBadge } from "@/components/applications/application-status-badge";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { ReportDialog } from "@/components/reports/report-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { queryKeys } from "@/lib/query-keys";
import { applicationService, projectService } from "@/services";
import type { ApplicationDto } from "@/types/application";

type Decision = "shortlist" | "interview" | "accept" | "reject";

export default function ReceivedApplicationsPage() { return <RoleGuard allowedRoles={[SystemRoles.Business, SystemRoles.Admin]}><ReceivedApplications /></RoleGuard>; }

function ReceivedApplications() {
  const queryClient = useQueryClient();
  const [projectId, setProjectId] = useState("");
  const [reason, setReason] = useState("");
  const ownedQuery = useQuery({ queryKey: [...queryKeys.projects, "owned"], queryFn: projectService.listOwnedProjects });
  const applicationsQuery = useQuery({ queryKey: [...queryKeys.applications, "received", projectId], queryFn: () => applicationService.listProjectApplications(projectId), enabled: Boolean(projectId) });
  const decisionMutation = useMutation({
    mutationFn: ({ application, action }: { application: ApplicationDto; action: Decision }) => applicationService.decide(application.projectId, application.id, action, { reason: reason.trim() || undefined }),
    onSuccess: () => { toast.success("Application updated."); setReason(""); void queryClient.invalidateQueries({ queryKey: [...queryKeys.applications, "received", projectId] }); },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  if (ownedQuery.isLoading) return <LoadingState label="Loading founder projects" />;
  const applications = applicationsQuery.data ?? [];
  return <div className="space-y-5"><div><h1 className="text-2xl font-semibold">Received Applications</h1><p className="mt-2 text-sm text-muted-foreground">Review applications received by projects you own.</p></div>
    <Panel><PanelBody className="grid gap-3 md:grid-cols-[1fr_1fr]"><label className="space-y-1.5 text-sm font-medium"><span>Project</span><select className="h-10 w-full rounded-md border border-input bg-background px-3" value={projectId} onChange={(event) => setProjectId(event.target.value)}><option value="">Select a project</option>{ownedQuery.data?.map((project) => <option key={project.id} value={project.id}>{project.title}</option>)}</select></label><label className="space-y-1.5 text-sm font-medium"><span>Decision reason</span><Input value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Optional founder note" /></label></PanelBody></Panel>
    {applicationsQuery.isLoading ? <LoadingState label="Loading received applications" /> : !projectId ? <EmptyState icon={ClipboardList} title="Select a project" description="Choose an owned project to review its applications." /> : !applications.length ? <EmptyState icon={ClipboardList} title="No applications" description="This project has not received applications yet." /> : <Panel><PanelHeader><PanelTitle>{applications.length} applications</PanelTitle></PanelHeader><PanelBody className="space-y-3">{applications.map((application) => <article key={application.id} className="rounded-md border border-border p-4"><div className="flex flex-col justify-between gap-4 lg:flex-row"><div><div className="flex flex-wrap items-center gap-2"><h2 className="font-semibold">{application.applicantFullName}</h2><ApplicationStatusBadge status={application.status} />{application.cvTitle ? <Badge tone="muted">{application.cvTitle}</Badge> : null}</div><p className="mt-1 text-xs text-muted-foreground">{application.applicantEmail}</p><p className="mt-3 line-clamp-3 text-sm text-muted-foreground">{application.coverLetter}</p></div><div className="flex shrink-0 flex-wrap gap-2"><Action icon={ListChecks} label="Shortlist" onClick={() => decisionMutation.mutate({ application, action: "shortlist" })} /><Action icon={MessageSquareText} label="Interview" onClick={() => decisionMutation.mutate({ application, action: "interview" })} /><Action icon={Check} label="Accept" onClick={() => decisionMutation.mutate({ application, action: "accept" })} /><Action icon={X} label="Reject" danger onClick={() => decisionMutation.mutate({ application, action: "reject" })} /><ReportDialog targetType="Application" targetId={application.id} /></div></div></article>)}</PanelBody></Panel>}
  </div>;
}

function Action({ icon: Icon, label, danger = false, onClick }: { icon: typeof Check; label: string; danger?: boolean; onClick: () => void }) { return <Button size="sm" variant={danger ? "danger" : "outline"} onClick={onClick}><Icon className="h-4 w-4" />{label}</Button>; }
