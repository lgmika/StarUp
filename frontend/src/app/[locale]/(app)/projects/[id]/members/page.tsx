"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { UserPlus, UsersRound } from "lucide-react";
import { ProjectWorkspaceNav } from "@/components/projects/project-workspace-nav";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { InlineError } from "@/components/ui/error-boundary";
import { EmptyState } from "@/components/workspace/empty-state";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { projectTeamService } from "@/services/project-team-service";

export default function ProjectMembersPage() {
  const { id } = useParams<{ id: string }>();

  const query = useQuery({
    queryKey: ["project-members", id],
    queryFn: () => projectTeamService.listMembers(id),
  });

  return (
    <div className="space-y-6">
      <PageHeader
        title="Project members"
        description="Active members and their project-level responsibilities."
        actions={
          <Link
            href="/team"
            className="inline-flex h-10 items-center gap-2 rounded-xl bg-primary px-4 text-sm font-semibold text-primary-foreground shadow-sm hover:bg-primary/90"
          >
            <UserPlus className="h-4 w-4" />
            Manage team
          </Link>
        }
      />
      
      <ProjectWorkspaceNav projectId={id} />

      {query.isLoading ? (
        <div className="grid gap-4 md:grid-cols-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-32 w-full rounded-2xl" />
          ))}
        </div>
      ) : query.error ? (
        <InlineError
          message={getApiErrorMessage(query.error)}
          onRetry={() => void query.refetch()}
        />
      ) : !query.data?.length ? (
        <EmptyState
          icon={UsersRound}
          title="No members"
          description="Invite collaborators from the team management page."
        />
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {query.data.map((member) => (
            <div
              key={member.id}
              className="rounded-2xl border border-border/60 bg-card p-5 shadow-sm transition-all hover:-translate-y-0.5 hover:shadow-md"
            >
              <div className="flex items-start justify-between gap-3">
                <div>
                  <Link
                    className="font-bold tracking-tight hover:text-primary"
                    href={`/members/${member.userId}`}
                  >
                    {member.fullName}
                  </Link>
                  <p className="mt-1 text-sm font-medium text-muted-foreground">
                    {member.email}
                  </p>
                </div>
                <Badge tone={member.isActive ? "success" : "muted"}>
                  {member.isActive ? "Active" : "Inactive"}
                </Badge>
              </div>
              <div className="mt-4 flex items-center justify-between border-t border-border pt-4">
                <Badge tone="default" className="bg-primary/10 text-primary">
                  {member.role}
                </Badge>
                <span className="text-xs font-medium text-muted-foreground/80">
                  Joined {new Date(member.joinedAt).toLocaleDateString()}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
