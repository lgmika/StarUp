"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FileCheck2, Loader2, LockKeyhole } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody } from "@/components/ui/panel";
import { getAccessToken } from "@/lib/auth";
import { getApiErrorMessage } from "@/lib/api";
import { queryKeys } from "@/lib/query-keys";
import { ndaService } from "@/services";

export function ProjectNdaPanel({ projectId, onAccepted }: { projectId: string; onAccepted?: () => void }) {
  const queryClient = useQueryClient();
  const [confirmed, setConfirmed] = useState(false);
  const ndaQuery = useQuery({ queryKey: [...queryKeys.nda, "project", projectId], queryFn: () => ndaService.getCurrentProjectNda(projectId), enabled: Boolean(getAccessToken()), retry: false });
  const acceptMutation = useMutation({
    mutationFn: () => ndaService.acceptProjectNda(projectId),
    onSuccess: () => { toast.success("NDA accepted."); void queryClient.invalidateQueries({ queryKey: [...queryKeys.nda, "project", projectId] }); onAccepted?.(); },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  if (!getAccessToken()) return <Panel className="border-amber-200"><PanelBody><div className="flex items-center gap-2 font-semibold"><LockKeyhole className="h-4 w-4" />NDA required</div><p className="mt-2 text-sm text-muted-foreground">Sign in to review and accept the current NDA.</p><Button className="mt-4" variant="outline" onClick={() => { window.location.href = `/auth/login?next=${encodeURIComponent(window.location.pathname)}`; }}>Sign in</Button></PanelBody></Panel>;
  if (ndaQuery.isLoading) return <Panel><PanelBody className="flex items-center gap-2 text-sm text-muted-foreground"><Loader2 className="h-4 w-4 animate-spin" />Loading NDA</PanelBody></Panel>;
  if (ndaQuery.error) return <Panel><PanelBody className="text-sm text-destructive">{getApiErrorMessage(ndaQuery.error)}</PanelBody></Panel>;
  const nda = ndaQuery.data;
  if (!nda?.requiresNda) return null;
  if (nda.alreadyAccepted) return <Panel className="border-emerald-200"><PanelBody className="flex items-center gap-2 text-sm font-medium text-emerald-700"><FileCheck2 className="h-4 w-4" />NDA version {nda.versionNumber} accepted</PanelBody></Panel>;

  return <Panel className="border-amber-200"><PanelBody><div className="flex items-center gap-2 font-semibold"><LockKeyhole className="h-4 w-4" />NDA version {nda.versionNumber}</div><div className="mt-3 max-h-64 overflow-y-auto whitespace-pre-wrap rounded-md border border-border bg-background p-3 text-sm text-muted-foreground">{nda.content}</div><label className="mt-4 flex items-start gap-2 text-sm"><input className="mt-0.5 h-4 w-4" type="checkbox" checked={confirmed} onChange={(event) => setConfirmed(event.target.checked)} /><span>I have read and agree to this NDA version.</span></label><Button className="mt-4" disabled={!confirmed || acceptMutation.isPending} onClick={() => acceptMutation.mutate()}>{acceptMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <FileCheck2 className="h-4 w-4" />}Accept NDA</Button></PanelBody></Panel>;
}
