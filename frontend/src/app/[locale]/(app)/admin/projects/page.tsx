"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Archive,
  Eye,
  EyeOff,
  FolderKanban,
  Search,
  Shield,
  XCircle,
  type LucideIcon,
} from "lucide-react";
import { toast } from "sonner";
import { RoleGuard } from "@/components/auth/role-guard";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { DataTable } from "@/components/ui/data-table";
import { InlineError } from "@/components/ui/error-boundary";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/workspace/status-badge";
import { getApiErrorMessage } from "@/lib/api";
import {
  PROJECT_STAGE_LABELS,
  PROJECT_STATUS_LABELS,
  PROJECT_VISIBILITY_LABELS,
  SystemRoles,
} from "@/lib/constants";
import { queryKeys } from "@/lib/query-keys";
import { adminService } from "@/services";
import { ProjectStage, ProjectStatus, ProjectVisibility } from "@/types/enums";
import type { AdminProjectDto } from "@/types/admin";

type ProjectAction = "hide" | "restore" | "archive" | "close";

export default function AdminProjectsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <AdminProjects />
    </RoleGuard>
  );
}

function AdminProjects() {
  const queryClient = useQueryClient();
  const [draftSearch, setDraftSearch] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [stage, setStage] = useState("");
  const [visibility, setVisibility] = useState("");
  const [page, setPage] = useState(1);
  const [pending, setPending] = useState<{
    project: AdminProjectDto;
    action: ProjectAction;
  } | null>(null);
  const [reason, setReason] = useState("");

  const projectsQuery = useQuery({
    queryKey: [
      ...queryKeys.admin,
      "projects",
      { search, status, stage, visibility, page },
    ],
    queryFn: () =>
      adminService.listProjects({
        search: search || undefined,
        status: (status as ProjectStatus) || undefined,
        stage: (stage as ProjectStage) || undefined,
        visibility: (visibility as ProjectVisibility) || undefined,
        page,
        pageSize: 20,
      }),
  });

  const actionMutation = useMutation({
    mutationFn: ({
      projectId,
      action,
      actionReason,
    }: {
      projectId: string;
      action: ProjectAction;
      actionReason: string;
    }) => adminService.projectAction(projectId, action, actionReason),
    onSuccess: () => {
      toast.success("Project action completed.");
      setPending(null);
      setReason("");
      void queryClient.invalidateQueries({
        queryKey: [...queryKeys.admin, "projects"],
      });
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

  const columns = [
    {
      key: "project",
      header: "Project",
      render: (project: AdminProjectDto) => (
        <div>
          <Link
            className="font-medium hover:text-primary"
            href={`/projects/${project.id}`}
          >
            {project.title}
          </Link>
          <p className="mt-1 max-w-xs truncate text-xs text-muted-foreground">
            {project.summary}
          </p>
        </div>
      ),
    },
    {
      key: "owner",
      header: "Owner",
      render: (project: AdminProjectDto) => (
        <div>
          <p className="text-sm font-medium">{project.ownerFullName}</p>
          <p className="text-xs text-muted-foreground">{project.ownerEmail}</p>
        </div>
      ),
    },
    {
      key: "status",
      header: "Status",
      render: (project: AdminProjectDto) => (
        <StatusBadge value={PROJECT_STATUS_LABELS[project.status]} />
      ),
    },
    {
      key: "stage",
      header: "Stage",
      render: (project: AdminProjectDto) => (
        <span className="text-sm">{PROJECT_STAGE_LABELS[project.stage]}</span>
      ),
    },
    {
      key: "visibility",
      header: "Visibility",
      render: (project: AdminProjectDto) => (
        <span className="text-sm">
          {PROJECT_VISIBILITY_LABELS[project.visibility]}
        </span>
      ),
    },
    {
      key: "recruiting",
      header: "Recruiting",
      render: (project: AdminProjectDto) => (
        <Badge tone={project.isRecruiting ? "success" : "muted"}>
          {project.isRecruiting ? "Open" : "Closed"}
        </Badge>
      ),
    },
    {
      key: "actions",
      header: "Actions",
      className: "text-right",
      render: (project: AdminProjectDto) => (
        <div className="flex justify-end gap-1">
          {project.status === ProjectStatus.Hidden ? (
            <ActionButton
              icon={Eye}
              label="Restore"
              onClick={() => setPending({ project, action: "restore" })}
            />
          ) : (
            <ActionButton
              icon={EyeOff}
              label="Hide"
              onClick={() => setPending({ project, action: "hide" })}
            />
          )}
          <ActionButton
            icon={XCircle}
            label="Close"
            onClick={() => setPending({ project, action: "close" })}
          />
          <ActionButton
            icon={Archive}
            label="Archive"
            onClick={() => setPending({ project, action: "archive" })}
          />
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <section className="relative overflow-hidden rounded-2xl border border-border/60 bg-gradient-to-r from-blue-500/[0.04] via-card to-card p-6 shadow-sm">
        <div className="pointer-events-none absolute -right-16 -top-16 h-40 w-40 rounded-full bg-blue-500/5 blur-3xl" />
        <div className="relative flex items-start justify-between gap-4">
          <div>
            <div className="flex items-center gap-2">
              <Shield className="h-4 w-4 text-blue-600" />
              <p className="text-sm font-medium text-muted-foreground">Admin</p>
            </div>
            <h1 className="mt-2 text-2xl font-bold tracking-tight">
              Project Management
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Search every project and apply audited administrative actions.
            </p>
          </div>
          <div className="flex items-center gap-2 rounded-xl bg-muted px-3 py-2 text-sm font-semibold">
            <FolderKanban className="h-4 w-4" />
            {result?.total ?? 0}
          </div>
        </div>
      </section>

      {/* Filters */}
      <div className="rounded-2xl border border-border/60 bg-card p-4 shadow-sm">
        <form
          className="grid gap-3 md:grid-cols-2 lg:grid-cols-[1fr_180px_160px_180px_auto]"
          onSubmit={submitSearch}
        >
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              className="rounded-xl pl-9"
              placeholder="Title or owner"
              value={draftSearch}
              onChange={(event) => setDraftSearch(event.target.value)}
            />
          </div>
          <Filter
            value={status}
            onChange={(value) => {
              setStatus(value);
              setPage(1);
            }}
            label="All statuses"
            options={Object.values(ProjectStatus)}
          />
          <Filter
            value={stage}
            onChange={(value) => {
              setStage(value);
              setPage(1);
            }}
            label="All stages"
            options={Object.values(ProjectStage)}
          />
          <Filter
            value={visibility}
            onChange={(value) => {
              setVisibility(value);
              setPage(1);
            }}
            label="All visibility"
            options={Object.values(ProjectVisibility)}
          />
          <Button type="submit" className="rounded-xl">
            <Search className="h-4 w-4" />
            Search
          </Button>
        </form>
      </div>

      {projectsQuery.error ? (
        <InlineError
          message={getApiErrorMessage(projectsQuery.error)}
          onRetry={() => void projectsQuery.refetch()}
        />
      ) : null}

      {/* Data Table */}
      {projectsQuery.isLoading ? (
        <div className="space-y-4">
          <Skeleton className="h-10 w-full rounded-xl" />
          <Skeleton className="h-64 w-full rounded-2xl" />
        </div>
      ) : (
        <div className="space-y-4">
          <DataTable
            columns={columns}
            data={result?.items ?? []}
            keyExtractor={(project) => project.id}
            emptyMessage="No projects match the current filters."
          />

          {/* Pagination */}
          {pages > 1 && (
            <div className="flex items-center justify-between px-2">
              <p className="text-sm text-muted-foreground">
                Page {page} of {pages}
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  className="rounded-lg"
                  disabled={page <= 1}
                  onClick={() => setPage((value) => value - 1)}
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  className="rounded-lg"
                  disabled={page >= pages}
                  onClick={() => setPage((value) => value + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Audit Action Footer */}
      {pending ? (
        <div className="fixed inset-x-0 bottom-0 z-[71] mx-auto w-full max-w-md p-4 animate-in slide-in-from-bottom-10">
          <label className="block rounded-xl border border-border/60 bg-card p-4 shadow-xl">
            <span className="text-sm font-semibold">
              Reason for {pending.action}
            </span>
            <Input
              className="mt-2 rounded-lg"
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Required audit reason"
              autoFocus
            />
          </label>
        </div>
      ) : null}

      <ConfirmDialog
        open={Boolean(pending)}
        title={`${
          pending?.action
            ? pending.action.charAt(0).toUpperCase() + pending.action.slice(1)
            : "Update"
        } project?`}
        description="This is an audited administrative action. Enter a reason before confirming."
        confirmLabel={
          pending?.action
            ? pending.action.charAt(0).toUpperCase() + pending.action.slice(1)
            : "Confirm"
        }
        isLoading={actionMutation.isPending}
        onClose={() => {
          setPending(null);
          setReason("");
        }}
        onConfirm={() =>
          pending && reason.trim()
            ? actionMutation.mutate({
                projectId: pending.project.id,
                action: pending.action,
                actionReason: reason.trim(),
              })
            : toast.error("Reason is required.")
        }
      />
    </div>
  );
}

function Filter({
  value,
  onChange,
  label,
  options,
}: {
  value: string;
  onChange: (value: string) => void;
  label: string;
  options: string[];
}) {
  return (
    <select
      className="h-10 rounded-xl border border-input bg-background px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      value={value}
      onChange={(event) => onChange(event.target.value)}
    >
      <option value="">{label}</option>
      {options.map((option) => (
        <option key={option} value={option}>
          {option}
        </option>
      ))}
    </select>
  );
}

function ActionButton({
  icon: Icon,
  label,
  onClick,
}: {
  icon: LucideIcon;
  label: string;
  onClick: () => void;
}) {
  return (
    <Button
      size="icon"
      variant="ghost"
      title={label}
      aria-label={label}
      onClick={onClick}
      className="h-8 w-8 rounded-lg hover:bg-muted"
    >
      <Icon className="h-4 w-4 text-muted-foreground" />
    </Button>
  );
}
