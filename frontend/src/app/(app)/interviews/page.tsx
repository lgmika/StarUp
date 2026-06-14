"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { CalendarClock, Check, ExternalLink, MapPin, Phone, Video, X } from "lucide-react";
import { toast } from "sonner";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { StatusBadge } from "@/components/workspace/status-badge";
import { getApiErrorMessage } from "@/lib/api";
import { interviewService } from "@/services";
import type { InterviewDto } from "@/types/interview";

const meetingIcons = {
  Online: Video,
  InPerson: MapPin,
  Phone,
};

const dateFormatter = new Intl.DateTimeFormat("vi-VN", {
  dateStyle: "medium",
  timeStyle: "short",
});

export default function InterviewsPage() {
  const [interviews, setInterviews] = useState<InterviewDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [mutatingId, setMutatingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const upcoming = useMemo(() => {
    const now = Date.now();
    return interviews
      .filter((interview) => new Date(interview.startAt).getTime() >= now && !["Cancelled", "Completed"].includes(interview.status))
      .sort((a, b) => new Date(a.startAt).getTime() - new Date(b.startAt).getTime());
  }, [interviews]);

  const history = useMemo(() => {
    const now = Date.now();
    return interviews
      .filter((interview) => new Date(interview.startAt).getTime() < now || ["Cancelled", "Completed"].includes(interview.status))
      .sort((a, b) => new Date(b.startAt).getTime() - new Date(a.startAt).getTime());
  }, [interviews]);

  async function loadInterviews() {
    try {
      setError(null);
      setIsLoading(true);
      setInterviews(await interviewService.listMine());
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
    } finally {
      setIsLoading(false);
    }
  }

  async function runAction(interviewId: string, action: "complete" | "cancel") {
    try {
      setMutatingId(interviewId);
      if (action === "complete") {
        await interviewService.complete(interviewId, { reason: "Marked complete from frontend console." });
        toast.success("Interview marked as completed.");
      } else {
        await interviewService.cancel(interviewId, { reason: "Cancelled from frontend console." });
        toast.success("Interview cancelled.");
      }
      await loadInterviews();
    } catch (mutationError) {
      toast.error(getApiErrorMessage(mutationError));
    } finally {
      setMutatingId(null);
    }
  }

  useEffect(() => {
    void loadInterviews();
  }, []);

  if (isLoading) return <LoadingState label="Loading interviews" />;

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold">Interviews</h1>
        <p className="mt-2 text-sm text-muted-foreground">Your scheduled interviews from /users/me/interviews.</p>
      </div>

      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}

      <div className="grid gap-3 md:grid-cols-3">
        <Metric label="Total" value={interviews.length} />
        <Metric label="Upcoming" value={upcoming.length} />
        <Metric label="History" value={history.length} />
      </div>

      {interviews.length === 0 ? (
        <EmptyState icon={CalendarClock} title="No interviews" description="When founders schedule interviews, they will appear here." />
      ) : (
        <div className="grid gap-5 xl:grid-cols-[1fr_420px]">
          <InterviewList title="Upcoming" interviews={upcoming} mutatingId={mutatingId} onAction={runAction} />
          <InterviewList title="History" interviews={history} mutatingId={mutatingId} onAction={runAction} readonly />
        </div>
      )}
    </div>
  );
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <Panel>
      <PanelBody>
        <p className="text-2xl font-semibold">{value}</p>
        <p className="mt-1 text-sm text-muted-foreground">{label}</p>
      </PanelBody>
    </Panel>
  );
}

function InterviewList({
  title,
  interviews,
  mutatingId,
  readonly = false,
  onAction,
}: {
  title: string;
  interviews: InterviewDto[];
  mutatingId: string | null;
  readonly?: boolean;
  onAction: (interviewId: string, action: "complete" | "cancel") => Promise<void>;
}) {
  return (
    <Panel>
      <PanelHeader>
        <PanelTitle>{title}</PanelTitle>
      </PanelHeader>
      <PanelBody className="space-y-3">
        {interviews.length === 0 ? <p className="py-8 text-center text-sm text-muted-foreground">No interviews in this section.</p> : null}
        {interviews.map((interview) => {
          const Icon = meetingIcons[interview.meetingType];
          return (
            <article key={interview.id} className="rounded-md border border-border p-4">
              <div className="flex flex-wrap items-center gap-2">
                <Icon className="h-4 w-4 text-muted-foreground" />
                <StatusBadge value={interview.status} />
                <Badge tone="muted">{interview.meetingType}</Badge>
              </div>
              <p className="mt-3 text-sm font-semibold">{dateFormatter.format(new Date(interview.startAt))}</p>
              <p className="mt-1 text-xs text-muted-foreground">
                Ends {dateFormatter.format(new Date(interview.endAt))} · {interview.timeZone}
              </p>
              {interview.note ? <p className="mt-3 text-sm text-muted-foreground">{interview.note}</p> : null}
              {interview.participants.length ? (
                <div className="mt-3 flex flex-wrap gap-2">
                  {interview.participants.map((participant) => (
                    <Badge key={participant.id} tone={participant.isRequired ? "default" : "muted"}>{participant.fullName}</Badge>
                  ))}
                </div>
              ) : null}
              <div className="mt-4 flex flex-wrap gap-2">
                <Link
                  href={`/applications/${interview.applicationId}`}
                  className="inline-flex h-8 items-center justify-center gap-2 rounded-md border border-border px-3 text-xs font-medium hover:bg-accent"
                >
                  <ExternalLink className="h-3.5 w-3.5" />
                  Application
                </Link>
                {interview.meetingUrl ? (
                  <a
                    href={interview.meetingUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="inline-flex h-8 items-center justify-center gap-2 rounded-md border border-border px-3 text-xs font-medium hover:bg-accent"
                  >
                    <ExternalLink className="h-3.5 w-3.5" />
                    Join
                  </a>
                ) : null}
                {!readonly && interview.status !== "Cancelled" ? (
                  <>
                    <Button size="sm" variant="outline" disabled={mutatingId === interview.id} onClick={() => void onAction(interview.id, "complete")}>
                      <Check className="h-4 w-4" />
                      Complete
                    </Button>
                    <Button size="sm" variant="danger" disabled={mutatingId === interview.id} onClick={() => void onAction(interview.id, "cancel")}>
                      <X className="h-4 w-4" />
                      Cancel
                    </Button>
                  </>
                ) : null}
              </div>
            </article>
          );
        })}
      </PanelBody>
    </Panel>
  );
}
