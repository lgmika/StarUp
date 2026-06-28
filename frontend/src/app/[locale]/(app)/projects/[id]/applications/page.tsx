"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CalendarPlus, Check, ClipboardList, ListChecks, X } from "lucide-react";
import { toast } from "sonner";
import { ApplicationStatusBadge } from "@/components/applications/application-status-badge";
import { ProjectWorkspaceNav } from "@/components/projects/project-workspace-nav";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { InlineError } from "@/components/ui/error-boundary";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/workspace/empty-state";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { applicationService } from "@/services/application-service";
import type { ApplicationDto } from "@/types/application";

type Decision = "shortlist" | "accept" | "reject";

export default function ProjectApplicationsPage() {
  const { id } = useParams<{ id: string }>();
  const client = useQueryClient();
  const key = ["project-applications", id];

  const query = useQuery({
    queryKey: key,
    queryFn: () => applicationService.listProjectApplications(id),
  });

  const mutation = useMutation({
    mutationFn: ({ application, action }: { application: ApplicationDto; action: Decision }) =>
      applicationService.decide(id, application.id, action, {}),
    onSuccess: () => {
      toast.success("Application updated.");
      void client.invalidateQueries({ queryKey: key });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader
        title="Project applications"
        description="Review applicants, inspect profiles, and progress candidates through hiring decisions."
      />
      <ProjectWorkspaceNav projectId={id} />

      {query.isLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-40 w-full rounded-2xl" />
          ))}
        </div>
      ) : query.error ? (
        <InlineError
          message={getApiErrorMessage(query.error)}
          onRetry={() => void query.refetch()}
        />
      ) : !query.data?.length ? (
        <EmptyState
          icon={ClipboardList}
          title="No applications"
          description="Applications submitted for this project will appear here."
        />
      ) : (
        <div className="space-y-4">
          {query.data.map((application) => (
            <div
              key={application.id}
              className="rounded-2xl border border-border/60 bg-card p-5 shadow-sm transition-all hover:shadow-md"
            >
              <div className="flex flex-col justify-between gap-6 lg:flex-row">
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <Link
                      href={`/members/${application.applicantUserId}`}
                      className="font-bold tracking-tight hover:text-primary"
                    >
                      {application.applicantFullName}
                    </Link>
                    <ApplicationStatusBadge status={application.status} />
                    {application.cvTitle ? (
                      <Badge tone="muted" className="bg-muted text-muted-foreground">
                        {application.cvTitle}
                      </Badge>
                    ) : null}
                  </div>
                  <p className="mt-1 text-xs font-medium text-muted-foreground">
                    {application.applicantEmail} · Applied{" "}
                    {new Date(application.createdAt).toLocaleDateString()}
                  </p>
                  <div className="mt-3 rounded-xl bg-muted/50 p-4">
                    <p className="line-clamp-3 text-sm leading-relaxed text-muted-foreground">
                      {application.coverLetter || <span className="italic">No cover letter provided.</span>}
                    </p>
                  </div>
                </div>
                <div className="flex shrink-0 flex-wrap gap-2 lg:flex-col lg:items-end">
                  <div className="flex w-full gap-2 lg:w-auto">
                    <Link
                      href={`/applications/${application.id}?projectId=${id}`}
                      className="inline-flex h-9 flex-1 items-center justify-center rounded-xl border border-border bg-background px-4 text-sm font-medium transition-colors hover:bg-accent lg:flex-none"
                    >
                      Details
                    </Link>
                    <Link
                      href={`/applications/${application.id}/interviews?projectId=${id}`}
                      className="inline-flex h-9 flex-1 items-center justify-center gap-2 rounded-xl border border-border bg-background px-4 text-sm font-medium transition-colors hover:bg-accent lg:flex-none"
                    >
                      <CalendarPlus className="h-4 w-4" />
                      Interviews
                    </Link>
                  </div>
                  <div className="flex w-full gap-2 lg:w-auto">
                    <Action
                      icon={ListChecks}
                      label="Shortlist"
                      disabled={mutation.isPending}
                      onClick={() => mutation.mutate({ application, action: "shortlist" })}
                    />
                    <Action
                      icon={Check}
                      label="Accept"
                      disabled={mutation.isPending}
                      onClick={() => mutation.mutate({ application, action: "accept" })}
                    />
                    <Action
                      icon={X}
                      label="Reject"
                      disabled={mutation.isPending}
                      onClick={() => mutation.mutate({ application, action: "reject" })}
                    />
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function Action({
  icon: Icon,
  label,
  disabled,
  onClick,
}: {
  icon: typeof Check;
  label: string;
  disabled: boolean;
  onClick: () => void;
}) {
  return (
    <Button
      size="sm"
      variant="outline"
      disabled={disabled}
      onClick={onClick}
      className="flex-1 rounded-xl lg:flex-none"
    >
      <Icon className="h-4 w-4" />
      {label}
    </Button>
  );
}
