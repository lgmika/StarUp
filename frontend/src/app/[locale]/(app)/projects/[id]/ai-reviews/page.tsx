"use client";

import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { BrainCircuit, Loader2, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { ProjectWorkspaceNav } from "@/components/projects/project-workspace-nav";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { InlineError } from "@/components/ui/error-boundary";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/workspace/empty-state";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { projectService } from "@/services/project-service";

export default function AiReviewsPage() {
  const { id } = useParams<{ id: string }>();
  const client = useQueryClient();
  const key = ["project-ai-reviews", id];

  const query = useQuery({
    queryKey: key,
    queryFn: () => projectService.getAiReviews(id),
  });

  const create = useMutation({
    mutationFn: () => projectService.createAiReview(id),
    onSuccess: () => {
      toast.success("AI review created.");
      void client.invalidateQueries({ queryKey: key });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader
        title="AI review history"
        description="Quality scoring, missing information, risk flags, and actionable suggestions."
        actions={
          <Button
            onClick={() => create.mutate()}
            disabled={create.isPending}
            className="rounded-xl shadow-sm"
          >
            {create.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <RefreshCw className="h-4 w-4" />
            )}
            Run new review
          </Button>
        }
      />
      
      <ProjectWorkspaceNav projectId={id} />

      {query.isLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 2 }).map((_, i) => (
            <Skeleton key={i} className="h-64 w-full rounded-2xl" />
          ))}
        </div>
      ) : query.error ? (
        <InlineError
          message={getApiErrorMessage(query.error)}
          onRetry={() => void query.refetch()}
        />
      ) : !query.data?.length ? (
        <EmptyState
          icon={BrainCircuit}
          title="No AI reviews"
          description="Run a review to assess project completeness and risk."
        />
      ) : (
        <div className="space-y-6">
          {query.data.map((review) => (
            <div
              key={review.id}
              className="rounded-2xl border border-border/60 bg-card shadow-sm transition-all hover:shadow-md"
            >
              <div className="flex items-center justify-between border-b border-border p-4">
                <div className="flex items-center gap-2 text-sm font-semibold">
                  <BrainCircuit className="h-4 w-4 text-primary" />
                  {new Date(review.createdAt).toLocaleString()}
                </div>
                <Badge
                  tone={
                    review.qualityScore >= 75
                      ? "success"
                      : review.qualityScore >= 50
                        ? "warning"
                        : "danger"
                  }
                  className="px-3 py-1 text-sm font-bold shadow-sm"
                >
                  Score: {review.qualityScore}
                </Badge>
              </div>
              <div className="space-y-5 p-5">
                <div className="rounded-xl bg-muted/50 p-4">
                  <p className="text-sm leading-relaxed text-foreground">
                    {review.summary}
                  </p>
                </div>
                
                <div className="grid gap-6 md:grid-cols-3">
                  <ReviewList
                    title="Missing information"
                    items={review.missingInformation}
                    tone="warning"
                  />
                  <ReviewList
                    title="Risk flags"
                    items={review.riskFlags}
                    tone="danger"
                  />
                  <ReviewList
                    title="Suggestions"
                    items={review.suggestions}
                    tone="default"
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

function ReviewList({
  title,
  items,
  tone,
}: {
  title: string;
  items: string[];
  tone: "warning" | "danger" | "default";
}) {
  if (!items.length) {
    return (
      <div>
        <p className="text-xs font-bold uppercase tracking-wider text-muted-foreground/70">
          {title}
        </p>
        <p className="mt-2 text-sm italic text-muted-foreground">None</p>
      </div>
    );
  }
  return (
    <div>
      <p className="text-xs font-bold uppercase tracking-wider text-muted-foreground/70">
        {title}
      </p>
      <div className="mt-3 flex flex-col gap-2">
        {items.map((item) => (
          <div
            key={item}
            className="flex items-start gap-2 text-sm leading-snug text-muted-foreground"
          >
            <div
              className={`mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full ${
                tone === "danger"
                  ? "bg-rose-500"
                  : tone === "warning"
                    ? "bg-amber-500"
                    : "bg-blue-500"
              }`}
            />
            <span>{item}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
