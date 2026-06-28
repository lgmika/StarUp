"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { useParams, useSearchParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, CalendarClock, Loader2, MapPin, Video } from "lucide-react";
import { toast } from "sonner";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { interviewService } from "@/services/interview-service";
import type { InterviewMeetingType } from "@/types/interview";

export default function ApplicationInterviewsPage() {
  const { id } = useParams<{ id: string }>(); const projectId = useSearchParams().get("projectId"); const client = useQueryClient(); const key = ["application-interviews", id];
  const [startAt, setStartAt] = useState(""); const [endAt, setEndAt] = useState(""); const [meetingType, setMeetingType] = useState<InterviewMeetingType>("Online"); const [meetingUrl, setMeetingUrl] = useState(""); const [location, setLocation] = useState(""); const [note, setNote] = useState("");
  const query = useQuery({ queryKey: key, queryFn: () => interviewService.listByApplication(id) });
  const schedule = useMutation({ mutationFn: () => interviewService.schedule(id, { startAt: new Date(startAt).toISOString(), endAt: new Date(endAt).toISOString(), timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone, meetingType, meetingUrl: meetingUrl || undefined, location: location || undefined, note: note || undefined }), onSuccess: () => { toast.success("Interview scheduled."); setStartAt(""); setEndAt(""); setNote(""); void client.invalidateQueries({ queryKey: key }); }, onError: (error) => toast.error(getApiErrorMessage(error)) });
  function submit(event: FormEvent) { event.preventDefault(); if (!startAt || !endAt || new Date(endAt) <= new Date(startAt)) { toast.error("Select a valid interview time range."); return; } schedule.mutate(); }
  if (query.isLoading) return <LoadingState label="Loading application interviews" />;
  return <div className="space-y-5"><Link href={projectId ? `/projects/${projectId}/applications` : "/applications"} className="inline-flex items-center gap-2 text-sm font-medium text-primary"><ArrowLeft className="h-4 w-4" />Back to applications</Link><PageHeader title="Application interviews" description="Schedule and track interview sessions for this candidate." />
    <Panel><PanelHeader><PanelTitle>Schedule interview</PanelTitle></PanelHeader><PanelBody><form className="grid gap-3 md:grid-cols-2" onSubmit={submit}><label className="space-y-1 text-sm font-medium">Starts<Input type="datetime-local" value={startAt} onChange={(event) => setStartAt(event.target.value)} /></label><label className="space-y-1 text-sm font-medium">Ends<Input type="datetime-local" value={endAt} onChange={(event) => setEndAt(event.target.value)} /></label><label className="space-y-1 text-sm font-medium">Meeting type<select className="h-10 w-full rounded-md border border-input bg-background px-3" value={meetingType} onChange={(event) => setMeetingType(event.target.value as InterviewMeetingType)}><option>Online</option><option>InPerson</option><option>Phone</option></select></label><label className="space-y-1 text-sm font-medium">{meetingType === "InPerson" ? "Location" : "Meeting URL"}<Input value={meetingType === "InPerson" ? location : meetingUrl} onChange={(event) => meetingType === "InPerson" ? setLocation(event.target.value) : setMeetingUrl(event.target.value)} /></label><label className="space-y-1 text-sm font-medium md:col-span-2">Note<textarea className="min-h-24 w-full rounded-md border border-input bg-background p-3 text-sm" value={note} onChange={(event) => setNote(event.target.value)} /></label><Button type="submit" className="md:w-fit" disabled={schedule.isPending}>{schedule.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <CalendarClock className="h-4 w-4" />}Schedule</Button></form></PanelBody></Panel>
    {query.error ? <p className="rounded-md bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(query.error)}</p> : null}{!query.data?.length ? <EmptyState icon={CalendarClock} title="No interviews" description="Schedule the first interview using the form above." /> : <div className="space-y-3">{query.data.map((interview) => <Panel key={interview.id}><PanelBody className="flex flex-col justify-between gap-3 md:flex-row"><div><div className="flex flex-wrap items-center gap-2"><p className="font-semibold">{new Date(interview.startAt).toLocaleString()}</p><Badge tone="muted">{interview.status}</Badge><Badge>{interview.meetingType}</Badge></div><p className="mt-2 flex items-center gap-1 text-sm text-muted-foreground">{interview.meetingType === "InPerson" ? <MapPin className="h-4 w-4" /> : <Video className="h-4 w-4" />}{interview.location ?? interview.meetingUrl ?? "Details pending"}</p>{interview.note ? <p className="mt-2 text-sm text-muted-foreground">{interview.note}</p> : null}</div><p className="text-xs text-muted-foreground">Ends {new Date(interview.endAt).toLocaleString()}</p></PanelBody></Panel>)}</div>}
  </div>;
}
