"use client";

import Link from "next/link";
import { useParams, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, Clock3, Loader2, RotateCcw } from "lucide-react";
import { toast } from "sonner";
import { useForm } from "react-hook-form";
import { ApplicationStatusBadge } from "@/components/applications/application-status-badge";
import { canWithdrawApplication, getApplicationStatusHint } from "@/components/applications/application-actions";
import { AuthMessage } from "@/components/auth/auth-message";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { withdrawApplicationSchema, type WithdrawApplicationFormValues } from "@/lib/validations/application";
import { applicationService } from "@/services";
import type { ApplicationDetailDto } from "@/types/application";

export default function ApplicationDetailPage() {
  const params = useParams<{ id: string }>();
  const searchParams = useSearchParams();
  const projectId = searchParams.get("projectId");
  const [detail, setDetail] = useState<ApplicationDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const form = useForm<WithdrawApplicationFormValues>({
    resolver: zodResolver(withdrawApplicationSchema),
    defaultValues: { reason: "" },
  });

  async function loadDetail() {
    if (!projectId) {
      setError("Missing projectId query parameter.");
      setIsLoading(false);
      return;
    }
    setIsLoading(true);
    setError(null);
    try {
      setDetail(await applicationService.getApplication(projectId, params.id));
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
    } finally {
      setIsLoading(false);
    }
  }

  async function withdraw(values: WithdrawApplicationFormValues) {
    if (!projectId || !detail) return;
    try {
      await applicationService.withdraw(projectId, detail.application.id, {
        reason: values.reason?.trim() || undefined,
      });
      toast.success("Application withdrawn.");
      form.reset({ reason: "" });
      await loadDetail();
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    }
  }

  useEffect(() => {
    void loadDetail();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params.id, projectId]);

  if (isLoading) return <LoadingState label="Loading application detail" />;

  if (error || !detail) {
    return (
      <div className="space-y-4">
        <AuthMessage tone="error">{error ?? "Application could not be loaded."}</AuthMessage>
        <Link className="inline-flex items-center gap-2 text-sm font-medium text-primary hover:underline" href="/applications">
          <ArrowLeft className="h-4 w-4" />
          Back to applications
        </Link>
      </div>
    );
  }

  const application = detail.application;
  const canWithdraw = canWithdrawApplication(application.status);

  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_380px]">
      <section className="space-y-5">
        <Link className="inline-flex items-center gap-2 text-sm font-medium text-primary hover:underline" href="/applications">
          <ArrowLeft className="h-4 w-4" />
          Back to applications
        </Link>
        <Panel>
          <PanelHeader>
            <PanelTitle>{application.projectTitle}</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-4">
            <div className="flex flex-wrap gap-2">
              <ApplicationStatusBadge status={application.status} />
              {application.cvTitle ? <span className="rounded-md bg-muted px-2 py-1 text-xs text-muted-foreground">{application.cvTitle}</span> : null}
            </div>
            <p className="text-sm leading-6 text-muted-foreground">{getApplicationStatusHint(application.status)}</p>
            <div>
              <p className="text-sm font-medium">Cover letter</p>
              <p className="mt-2 whitespace-pre-wrap rounded-md border border-border bg-background p-3 text-sm leading-6 text-muted-foreground">{application.coverLetter}</p>
            </div>
            {application.founderNote ? (
              <div>
                <p className="text-sm font-medium">Founder note</p>
                <p className="mt-2 rounded-md border border-border bg-background p-3 text-sm leading-6 text-muted-foreground">{application.founderNote}</p>
              </div>
            ) : null}
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader>
            <PanelTitle>Status history</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-3">
            {detail.statusHistory.map((history) => (
              <div key={history.id} className="flex gap-3 rounded-md border border-border p-3">
                <Clock3 className="mt-0.5 h-4 w-4 text-muted-foreground" />
                <div>
                  <p className="text-sm font-medium">{history.fromStatus} → {history.toStatus}</p>
                  {history.reason ? <p className="mt-1 text-sm text-muted-foreground">{history.reason}</p> : null}
                  <p className="mt-1 text-xs text-muted-foreground">{new Date(history.createdAt).toLocaleString()}</p>
                </div>
              </div>
            ))}
          </PanelBody>
        </Panel>
      </section>

      <aside>
        <Panel>
          <PanelHeader>
            <PanelTitle>Withdraw application</PanelTitle>
          </PanelHeader>
          <PanelBody>
            <form className="space-y-4" onSubmit={form.handleSubmit(withdraw)}>
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Reason</span>
                <textarea className="min-h-28 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring" disabled={!canWithdraw} {...form.register("reason")} />
              </label>
              <Button type="submit" variant="outline" disabled={!canWithdraw || form.formState.isSubmitting}>
                {form.formState.isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <RotateCcw className="h-4 w-4" />}
                Withdraw
              </Button>
              {!canWithdraw ? <p className="text-xs text-muted-foreground">This application can no longer be withdrawn.</p> : null}
            </form>
          </PanelBody>
        </Panel>
      </aside>
    </div>
  );
}
