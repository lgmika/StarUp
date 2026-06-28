"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, CheckCircle2, EyeOff, Loader2, RotateCcw, Send, XCircle } from "lucide-react";
import { toast } from "sonner";
import { useForm } from "react-hook-form";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import {
  canRunModerationAction,
  moderationActionLabels,
  type ModerationAction,
} from "@/components/moderator/moderation-actions";
import { RiskFlags } from "@/components/moderator/risk-flags";
import { ProjectStageBadge, ProjectStatusBadge } from "@/components/projects/project-badges";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { moderationDecisionSchema, type ModerationDecisionFormValues } from "@/lib/validations/moderation";
import { moderatorService } from "@/services";
import type { ModeratorProjectDetailDto } from "@/types/moderator";

const actions: Array<{ action: ModerationAction; icon: typeof CheckCircle2; variant?: "outline" | "danger" }> = [
  { action: "approve", icon: CheckCircle2 },
  { action: "request-improvement", icon: Send, variant: "outline" },
  { action: "reject", icon: XCircle, variant: "danger" },
  { action: "hide", icon: EyeOff, variant: "outline" },
  { action: "restore", icon: RotateCcw, variant: "outline" },
];

export default function ModeratorProjectDetailPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Moderator, SystemRoles.Admin]}>
      <ModeratorProjectDetail />
    </RoleGuard>
  );
}

function ModeratorProjectDetail() {
  const params = useParams<{ id: string }>();
  const [project, setProject] = useState<ModeratorProjectDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [runningAction, setRunningAction] = useState<ModerationAction | null>(null);
  const [error, setError] = useState<string | null>(null);
  const form = useForm<ModerationDecisionFormValues>({
    resolver: zodResolver(moderationDecisionSchema),
    defaultValues: { reason: "" },
  });

  async function loadProject() {
    setIsLoading(true);
    setError(null);
    try {
      setProject(await moderatorService.getProject(params.id));
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
    } finally {
      setIsLoading(false);
    }
  }

  async function runAction(action: ModerationAction, values: ModerationDecisionFormValues) {
    setRunningAction(action);
    try {
      if (action === "approve") await moderatorService.approve(params.id, values);
      if (action === "request-improvement") await moderatorService.requestImprovement(params.id, values);
      if (action === "reject") await moderatorService.reject(params.id, values);
      if (action === "hide") await moderatorService.hide(params.id, values);
      if (action === "restore") await moderatorService.restore(params.id, values);
      toast.success(`${moderationActionLabels[action]} completed.`);
      form.reset({ reason: "" });
      await loadProject();
    } catch (actionError) {
      toast.error(getApiErrorMessage(actionError));
    } finally {
      setRunningAction(null);
    }
  }

  useEffect(() => {
    void loadProject();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params.id]);

  if (isLoading) return <LoadingState label="Loading moderation detail" />;

  if (error || !project) {
    return (
      <div className="space-y-4">
        <p className="text-sm text-destructive">{error ?? "Project could not be loaded."}</p>
        <Link className="inline-flex items-center gap-2 text-sm font-medium text-primary hover:underline" href="/moderator/projects/pending">
          <ArrowLeft className="h-4 w-4" />
          Back to queue
        </Link>
      </div>
    );
  }

  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_380px]">
      <section className="space-y-5">
        <Link className="inline-flex items-center gap-2 text-sm font-medium text-primary hover:underline" href="/moderator/projects/pending">
          <ArrowLeft className="h-4 w-4" />
          Back to queue
        </Link>
        <Panel>
          <PanelHeader>
            <PanelTitle>{project.title}</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-4">
            <div className="flex flex-wrap gap-2">
              <ProjectStatusBadge status={project.status} />
              <ProjectStageBadge stage={project.stage} />
              {project.latestAIQualityScore != null ? <Badge tone="default">AI score {project.latestAIQualityScore}</Badge> : <Badge tone="muted">No AI score</Badge>}
            </div>
            <p className="text-sm leading-6 text-muted-foreground">{project.summary}</p>
            <div className="grid gap-4 md:grid-cols-2">
              <DetailBlock title="Problem" text={project.problem} />
              <DetailBlock title="Solution" text={project.solution} />
            </div>
            <div>
              <p className="text-sm font-medium">AI risk flags</p>
              <div className="mt-2">
                <RiskFlags flags={project.latestAIRiskFlags} />
              </div>
            </div>
            <p className="text-xs text-muted-foreground">Owner: {project.ownerEmail}</p>
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader>
            <PanelTitle>Moderation history</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-3">
            {project.moderationHistory.length === 0 ? <p className="text-sm text-muted-foreground">No moderation history yet.</p> : null}
            {project.moderationHistory.map((review) => (
              <div key={review.id} className="rounded-md border border-border p-3">
                <div className="flex flex-wrap items-center gap-2">
                  <Badge tone="muted">{review.decision}</Badge>
                  {review.aiQualityScoreSnapshot != null ? <Badge tone="default">AI {review.aiQualityScoreSnapshot}</Badge> : null}
                </div>
                <p className="mt-2 text-sm leading-6 text-muted-foreground">{review.reason}</p>
                <p className="mt-1 text-xs text-muted-foreground">{new Date(review.createdAt).toLocaleString()}</p>
              </div>
            ))}
          </PanelBody>
        </Panel>
      </section>

      <aside>
        <Panel>
          <PanelHeader>
            <PanelTitle>Decision</PanelTitle>
          </PanelHeader>
          <PanelBody>
            <form className="space-y-4">
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Reason</span>
                <textarea className="min-h-36 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring" {...form.register("reason")} />
                {form.formState.errors.reason ? <span className="text-xs text-destructive">{form.formState.errors.reason.message}</span> : null}
              </label>
              <div className="grid gap-2">
                {actions.map((item) => {
                  const Icon = item.icon;
                  const enabled = canRunModerationAction(project.status, item.action);
                  const isRunning = runningAction === item.action;
                  return (
                    <Button
                      key={item.action}
                      type="button"
                      variant={item.variant}
                      disabled={!enabled || !!runningAction}
                      onClick={() => void form.handleSubmit((values) => runAction(item.action, values))()}
                    >
                      {isRunning ? <Loader2 className="h-4 w-4 animate-spin" /> : <Icon className="h-4 w-4" />}
                      {moderationActionLabels[item.action]}
                    </Button>
                  );
                })}
              </div>
              <p className="text-xs text-muted-foreground">Unavailable actions are locked by current project status. Backend still validates every transition.</p>
            </form>
          </PanelBody>
        </Panel>
      </aside>
    </div>
  );
}

function DetailBlock({ title, text }: { title: string; text: string }) {
  return (
    <div className="rounded-md border border-border p-3">
      <p className="text-sm font-medium">{title}</p>
      <p className="mt-2 text-sm leading-6 text-muted-foreground">{text}</p>
    </div>
  );
}
