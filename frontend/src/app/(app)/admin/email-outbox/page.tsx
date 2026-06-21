"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Mail, RefreshCw, Search } from "lucide-react";
import { toast } from "sonner";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { queryKeys } from "@/lib/query-keys";
import { adminService } from "@/services";
import type { AdminEmailOutboxDto } from "@/types/admin";

export default function EmailOutboxPage() {
  return <RoleGuard allowedRoles={[SystemRoles.Admin]}><EmailOutbox /></RoleGuard>;
}

function EmailOutbox() {
  const queryClient = useQueryClient();
  const [recipient, setRecipient] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [retryMessage, setRetryMessage] = useState<AdminEmailOutboxDto | null>(null);
  const query = useQuery({ queryKey: [...queryKeys.admin, "email-outbox", recipient, status, page], queryFn: () => adminService.listEmailOutbox({ recipient: recipient || undefined, status: status || undefined, page, pageSize: 20 }) });
  const retryMutation = useMutation({
    mutationFn: async () => {
      if (!retryMessage) throw new Error("Select an email to retry");
      return adminService.retryEmail(retryMessage.id, "Manual retry from admin console");
    },
    onSuccess: () => { toast.success("Email queued for retry."); setRetryMessage(null); void queryClient.invalidateQueries({ queryKey: [...queryKeys.admin, "email-outbox"] }); },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });
  const result = query.data;
  return <div className="space-y-5">
    <PageHeader title="Email Outbox" description="Inspect queued email delivery and retry failed messages." />
    <div className="flex flex-col gap-3 md:flex-row"><div className="relative flex-1"><Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" /><Input className="pl-9" placeholder="Filter recipient" value={recipient} onChange={(event) => { setRecipient(event.target.value); setPage(1); }} /></div><select className="h-10 rounded-md border border-input bg-background px-3 text-sm" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1); }}><option value="">All statuses</option><option value="Pending">Pending</option><option value="Processing">Processing</option><option value="Sent">Sent</option><option value="Failed">Failed</option></select></div>
    {query.isLoading ? <LoadingState label="Loading email outbox" /> : query.error ? <p className="rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(query.error)}</p> : !result?.items.length ? <EmptyState icon={Mail} title="Outbox is empty" description="No messages match the current filters." /> : <div className="grid gap-3">{result.items.map((message) => <Panel key={message.id}><PanelBody className="flex flex-col justify-between gap-4 md:flex-row md:items-center"><div className="min-w-0"><div className="flex flex-wrap items-center gap-2"><p className="truncate font-medium">{message.recipient}</p><Badge tone={statusTone(message.status)}>{message.status}</Badge></div><p className="mt-1 text-sm text-muted-foreground">{message.template} / {message.attempts} attempt(s)</p>{message.lastError ? <p className="mt-2 text-xs text-destructive">{message.lastError}</p> : null}</div>{message.status !== "Sent" ? <Button variant="outline" onClick={() => setRetryMessage(message)}><RefreshCw className="h-4 w-4" />Retry</Button> : null}</PanelBody></Panel>)}</div>}
    {result && result.total > result.pageSize ? <div className="flex items-center justify-end gap-2"><Button variant="outline" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Previous</Button><span className="text-sm text-muted-foreground">Page {page}</span><Button variant="outline" disabled={page * result.pageSize >= result.total} onClick={() => setPage((value) => value + 1)}>Next</Button></div> : null}
    <ConfirmDialog open={Boolean(retryMessage)} title="Retry email?" description={`Queue another delivery attempt for ${retryMessage?.recipient ?? "this recipient"}.`} confirmLabel="Retry email" isLoading={retryMutation.isPending} onClose={() => setRetryMessage(null)} onConfirm={() => retryMutation.mutate()} />
  </div>;
}

function statusTone(status: string): "success" | "warning" | "danger" | "muted" { if (status === "Sent") return "success"; if (status === "Failed") return "danger"; if (status === "Pending") return "warning"; return "muted"; }
