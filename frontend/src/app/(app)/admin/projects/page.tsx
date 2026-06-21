"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Archive, Eye, EyeOff, FolderKanban, LockKeyhole, Search, XCircle } from "lucide-react";
import { toast } from "sonner";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { StatusBadge } from "@/components/workspace/status-badge";
import { getApiErrorMessage } from "@/lib/api";
import { PROJECT_STAGE_LABELS, PROJECT_STATUS_LABELS, PROJECT_VISIBILITY_LABELS, SystemRoles } from "@/lib/constants";
import { queryKeys } from "@/lib/query-keys";
import { adminService } from "@/services";
import { ProjectStage, ProjectStatus, ProjectVisibility } from "@/types/enums";
import type { AdminProjectDto } from "@/types/admin";

type ProjectAction = "hide" | "restore" | "archive" | "close";

export default function AdminProjectsPage() {
  return <RoleGuard allowedRoles={[SystemRoles.Admin]}><AdminProjects /></RoleGuard>;
}

function AdminProjects() {
  const queryClient = useQueryClient();
  const [draftSearch, setDraftSearch] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [stage, setStage] = useState("");
  const [visibility, setVisibility] = useState("");
  const [page, setPage] = useState(1);
  const [pending, setPending] = useState<{ project: AdminProjectDto; action: ProjectAction } | null>(null);
  const [reason, setReason] = useState("");

  const projectsQuery = useQuery({
    queryKey: [...queryKeys.admin, "projects", { search, status, stage, visibility, page }],
    queryFn: () => adminService.listProjects({ search: search || undefined, status: status as ProjectStatus || undefined, stage: stage as ProjectStage || undefined, visibility: visibility as ProjectVisibility || undefined, page, pageSize: 20 }),
  });
  const actionMutation = useMutation({
    mutationFn: ({ projectId, action, actionReason }: { projectId: string; action: ProjectAction; actionReason: string }) => adminService.projectAction(projectId, action, actionReason),
    onSuccess: () => {
      toast.success("Project action completed.");
      setPending(null);
      setReason("");
      void queryClient.invalidateQueries({ queryKey: [...queryKeys.admin, "projects"] });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  function submitSearch(event: FormEvent) {
    event.preventDefault();
    setPage(1);
    setSearch(draftSearch.trim());
  }

  const result = projectsQuery.data;
  const pages = result ? Math.max(1, Math.ceil(result.total / result.pageSize)) : 1;

  return (
    <div className="space-y-5">
      <div><h1 className="text-2xl font-semibold">Admin Projects</h1><p className="mt-2 text-sm text-muted-foreground">Search every project and apply audited administrative actions.</p></div>
      <Panel><PanelBody>
        <form className="grid gap-3 lg:grid-cols-[1fr_180px_160px_180px_auto]" onSubmit={submitSearch}>
          <div className="relative"><Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" /><Input className="pl-9" placeholder="Title or owner" value={draftSearch} onChange={(event) => setDraftSearch(event.target.value)} /></div>
          <Filter value={status} onChange={(value) => { setStatus(value); setPage(1); }} label="All statuses" options={Object.values(ProjectStatus)} />
          <Filter value={stage} onChange={(value) => { setStage(value); setPage(1); }} label="All stages" options={Object.values(ProjectStage)} />
          <Filter value={visibility} onChange={(value) => { setVisibility(value); setPage(1); }} label="All visibility" options={Object.values(ProjectVisibility)} />
          <Button type="submit"><Search className="h-4 w-4" />Search</Button>
        </form>
      </PanelBody></Panel>
      {projectsQuery.error ? <p className="rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(projectsQuery.error)}</p> : null}
      {projectsQuery.isLoading ? <LoadingState label="Loading projects" /> : !result?.items.length ? <EmptyState icon={FolderKanban} title="No projects" description="No projects match the current filters." /> : (
        <Panel><PanelHeader><PanelTitle>{result.total} projects</PanelTitle></PanelHeader><PanelBody className="overflow-x-auto">
          <table className="w-full min-w-[1180px] text-left text-sm"><thead className="border-b border-border text-xs text-muted-foreground"><tr><th className="py-2">Project</th><th>Owner</th><th>Status</th><th>Stage</th><th>Visibility</th><th>Recruiting</th><th className="text-right">Actions</th></tr></thead>
            <tbody>{result.items.map((project) => <tr key={project.id} className="border-b border-border">
              <td className="py-3"><Link className="font-medium hover:text-primary" href={`/projects/${project.id}`}>{project.title}</Link><p className="mt-1 max-w-xs truncate text-xs text-muted-foreground">{project.summary}</p></td>
              <td><p>{project.ownerFullName}</p><p className="text-xs text-muted-foreground">{project.ownerEmail}</p></td>
              <td><StatusBadge value={PROJECT_STATUS_LABELS[project.status]} /></td><td>{PROJECT_STAGE_LABELS[project.stage]}</td><td>{PROJECT_VISIBILITY_LABELS[project.visibility]}</td><td><Badge tone={project.isRecruiting ? "success" : "muted"}>{project.isRecruiting ? "Open" : "Closed"}</Badge></td>
              <td><div className="flex justify-end gap-1">
                {project.status === ProjectStatus.Hidden ? <ActionButton icon={Eye} label="Restore" onClick={() => setPending({ project, action: "restore" })} /> : <ActionButton icon={EyeOff} label="Hide" onClick={() => setPending({ project, action: "hide" })} />}
                <ActionButton icon={XCircle} label="Close" onClick={() => setPending({ project, action: "close" })} />
                <ActionButton icon={Archive} label="Archive" onClick={() => setPending({ project, action: "archive" })} />
              </div></td>
            </tr>)}</tbody>
          </table>
          <div className="mt-4 flex items-center justify-between"><p className="text-xs text-muted-foreground">Page {page} of {pages}</p><div className="flex gap-2"><Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Previous</Button><Button variant="outline" size="sm" disabled={page >= pages} onClick={() => setPage((value) => value + 1)}>Next</Button></div></div>
        </PanelBody></Panel>
      )}
      {pending ? <div className="fixed inset-x-0 bottom-0 z-[71] mx-auto w-full max-w-md p-4"><label className="block rounded-md border border-border bg-card p-3 shadow-lg"><span className="text-xs font-medium">Reason for {pending.action}</span><Input className="mt-2" value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Required audit reason" /></label></div> : null}
      <ConfirmDialog open={Boolean(pending)} title={`${pending?.action ?? "Update"} project?`} description="This is an audited administrative action. Enter a reason before confirming." confirmLabel={pending?.action ?? "Confirm"} isLoading={actionMutation.isPending} onClose={() => { setPending(null); setReason(""); }} onConfirm={() => pending && reason.trim() ? actionMutation.mutate({ projectId: pending.project.id, action: pending.action, actionReason: reason.trim() }) : toast.error("Reason is required.")} />
    </div>
  );
}

function Filter({ value, onChange, label, options }: { value: string; onChange: (value: string) => void; label: string; options: string[] }) {
  return <select className="h-10 rounded-md border border-input bg-background px-3 text-sm" value={value} onChange={(event) => onChange(event.target.value)}><option value="">{label}</option>{options.map((option) => <option key={option} value={option}>{option}</option>)}</select>;
}

function ActionButton({ icon: Icon, label, onClick }: { icon: typeof LockKeyhole; label: string; onClick: () => void }) {
  return <Button size="icon" variant="ghost" title={label} aria-label={label} onClick={onClick}><Icon className="h-4 w-4" /></Button>;
}
