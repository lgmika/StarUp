"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Flag, Loader2, X } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { getAccessToken } from "@/lib/auth";
import { getApiErrorMessage } from "@/lib/api";
import { REPORT_REASON_LABELS } from "@/lib/constants";
import { queryKeys } from "@/lib/query-keys";
import { reportService } from "@/services";
import { ReportReasonCode } from "@/types/enums";

export function ReportDialog({ targetType, targetId, label = "Report" }: { targetType: string; targetId: string; label?: string }) {
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [reasonCode, setReasonCode] = useState(ReportReasonCode.Other);
  const [description, setDescription] = useState("");
  const [evidence, setEvidence] = useState("");
  const contextQuery = useQuery({ queryKey: [...queryKeys.reports, "target", targetType, targetId], queryFn: () => reportService.getTargetContext(targetType, targetId), enabled: open && Boolean(getAccessToken()) });
  const createMutation = useMutation({
    mutationFn: () => reportService.create({ targetType, targetId, reasonCode, description: description.trim(), evidence: evidence.trim() || undefined }),
    onSuccess: () => {
      toast.success("Report submitted.");
      setOpen(false);
      setDescription("");
      setEvidence("");
      void queryClient.invalidateQueries({ queryKey: queryKeys.reports });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  function show() {
    if (!getAccessToken()) {
      window.location.href = `/auth/login?next=${encodeURIComponent(window.location.pathname)}`;
      return;
    }
    setOpen(true);
  }

  return (
    <>
      <Button variant="outline" onClick={show}><Flag className="h-4 w-4" />{label}</Button>
      {open ? <div className="fixed inset-0 z-[70] flex items-center justify-center p-4" role="dialog" aria-modal="true" aria-label={`Report ${targetType}`}>
        <button type="button" className="absolute inset-0 bg-black/45" aria-label="Close report dialog" onClick={() => setOpen(false)} />
        <div className="relative w-full max-w-lg rounded-md border border-border bg-card p-5 shadow-xl">
          <div className="flex items-start justify-between gap-3"><div><h2 className="font-semibold">Report {targetType}</h2><p className="mt-1 text-sm text-muted-foreground">{contextQuery.data?.displayName ?? "Validating report target..."}</p></div><button type="button" className="flex h-9 w-9 items-center justify-center rounded-md hover:bg-accent" onClick={() => setOpen(false)} aria-label="Close"><X className="h-4 w-4" /></button></div>
          {contextQuery.error ? <p className="mt-4 rounded-md bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(contextQuery.error)}</p> : null}
          {contextQuery.data && !contextQuery.data.canReport ? <p className="mt-4 rounded-md bg-amber-50 p-3 text-sm text-amber-800">{contextQuery.data.reason ?? "This target cannot be reported."}</p> : null}
          <div className="mt-4 space-y-4">
            <label className="block space-y-1.5 text-sm font-medium"><span>Reason</span><select className="h-10 w-full rounded-md border border-input bg-background px-3" value={reasonCode} onChange={(event) => setReasonCode(event.target.value as ReportReasonCode)}>{Object.values(ReportReasonCode).map((reason) => <option key={reason} value={reason}>{REPORT_REASON_LABELS[reason]}</option>)}</select></label>
            <label className="block space-y-1.5 text-sm font-medium"><span>Description</span><textarea className="min-h-28 w-full rounded-md border border-input bg-background px-3 py-2" value={description} maxLength={2000} onChange={(event) => setDescription(event.target.value)} /></label>
            <label className="block space-y-1.5 text-sm font-medium"><span>Evidence URL or notes (optional)</span><Input value={evidence} maxLength={2000} onChange={(event) => setEvidence(event.target.value)} /></label>
          </div>
          <div className="mt-5 flex justify-end gap-2"><Button variant="outline" onClick={() => setOpen(false)}>Cancel</Button><Button variant="danger" disabled={!contextQuery.data?.canReport || description.trim().length < 5 || createMutation.isPending} onClick={() => createMutation.mutate()}>{createMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Flag className="h-4 w-4" />}Submit report</Button></div>
        </div>
      </div> : null}
    </>
  );
}
