"use client";

import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, Handshake, Info, X } from "lucide-react";
import { toast } from "sonner";
import { ProjectWorkspaceNav } from "@/components/projects/project-workspace-nav";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { InlineError } from "@/components/ui/error-boundary";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/workspace/empty-state";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { investorService } from "@/services/investor-service";

type Decision = "accept" | "reject" | "request-more-info";

export default function ProjectInvestorInterestsPage() {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const key = ["project-investor-interests", id];

  const query = useQuery({
    queryKey: key,
    queryFn: () => investorService.listProjectInterests(id),
  });

  const mutation = useMutation({
    mutationFn: ({ interestId, action }: { interestId: string; action: Decision }) =>
      investorService.decideInterest(id, interestId, action),
    onSuccess: () => {
      toast.success("Investor interest updated.");
      void queryClient.invalidateQueries({ queryKey: key });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader
        title="Investor interests"
        description="Review inbound investor interest and move each conversation forward."
      />
      <ProjectWorkspaceNav projectId={id} />

      {query.isLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
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
          icon={Handshake}
          title="No investor interests"
          description="New investor enquiries for this project will appear here."
        />
      ) : (
        <div className="space-y-4">
          {query.data.map((interest) => (
            <div
              key={interest.id}
              className="rounded-2xl border border-border/60 bg-card p-5 shadow-sm transition-all hover:shadow-md"
            >
              <div className="flex flex-col justify-between gap-4 md:flex-row">
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="font-bold tracking-tight text-foreground">
                      {interest.investorEmail}
                    </p>
                    <Badge tone="muted" className="bg-muted text-muted-foreground">
                      {interest.status}
                    </Badge>
                  </div>
                  <div className="mt-3 rounded-xl bg-muted/50 p-4">
                    <p className="text-sm leading-relaxed text-muted-foreground">
                      {interest.message}
                    </p>
                  </div>
                  {interest.founderResponse ? (
                    <div className="mt-3 rounded-xl border border-primary/20 bg-primary/5 p-4">
                      <p className="text-xs font-semibold text-primary uppercase tracking-wider mb-1">
                        Your Response
                      </p>
                      <p className="text-sm text-foreground">
                        {interest.founderResponse}
                      </p>
                    </div>
                  ) : null}
                  <p className="mt-3 text-xs font-medium text-muted-foreground/80">
                    Received {new Date(interest.createdAt).toLocaleString()}
                  </p>
                </div>
                <div className="flex shrink-0 flex-wrap gap-2 md:flex-col lg:flex-row">
                  <DecisionButton
                    label="Accept"
                    icon={Check}
                    onClick={() => mutation.mutate({ interestId: interest.id, action: "accept" })}
                    disabled={mutation.isPending}
                  />
                  <DecisionButton
                    label="More info"
                    icon={Info}
                    onClick={() =>
                      mutation.mutate({ interestId: interest.id, action: "request-more-info" })
                    }
                    disabled={mutation.isPending}
                  />
                  <DecisionButton
                    label="Reject"
                    icon={X}
                    onClick={() => mutation.mutate({ interestId: interest.id, action: "reject" })}
                    disabled={mutation.isPending}
                  />
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function DecisionButton({
  label,
  icon: Icon,
  onClick,
  disabled,
}: {
  label: string;
  icon: typeof Check;
  onClick: () => void;
  disabled: boolean;
}) {
  return (
    <Button
      size="sm"
      variant="outline"
      onClick={onClick}
      disabled={disabled}
      className="flex-1 rounded-xl md:flex-none"
    >
      <Icon className="h-4 w-4" />
      {label}
    </Button>
  );
}
