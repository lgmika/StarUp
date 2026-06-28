"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Mail, RefreshCw, Search, SendToBack } from "lucide-react";
import { toast } from "sonner";
import { RoleGuard } from "@/components/auth/role-guard";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { DataTable } from "@/components/ui/data-table";
import { InlineError } from "@/components/ui/error-boundary";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { queryKeys } from "@/lib/query-keys";
import { adminService } from "@/services";
import type { AdminEmailOutboxDto } from "@/types/admin";

export default function EmailOutboxPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <EmailOutbox />
    </RoleGuard>
  );
}

function EmailOutbox() {
  const queryClient = useQueryClient();
  const [recipient, setRecipient] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [retryMessage, setRetryMessage] = useState<AdminEmailOutboxDto | null>(
    null
  );

  const query = useQuery({
    queryKey: [...queryKeys.admin, "email-outbox", recipient, status, page],
    queryFn: () =>
      adminService.listEmailOutbox({
        recipient: recipient || undefined,
        status: status || undefined,
        page,
        pageSize: 20,
      }),
  });

  const retryMutation = useMutation({
    mutationFn: async () => {
      if (!retryMessage) throw new Error("Select an email to retry");
      return adminService.retryEmail(
        retryMessage.id,
        "Manual retry from admin console"
      );
    },
    onSuccess: () => {
      toast.success("Email queued for retry.");
      setRetryMessage(null);
      void queryClient.invalidateQueries({
        queryKey: [...queryKeys.admin, "email-outbox"],
      });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  const result = query.data;

  const columns = [
    {
      key: "recipient",
      header: "Recipient",
      render: (message: AdminEmailOutboxDto) => (
        <div className="min-w-0">
          <p className="truncate font-medium">{message.recipient}</p>
          <p className="mt-1 text-xs text-muted-foreground">
            Template: {message.template}
          </p>
        </div>
      ),
    },
    {
      key: "status",
      header: "Status",
      render: (message: AdminEmailOutboxDto) => (
        <Badge tone={statusTone(message.status)}>{message.status}</Badge>
      ),
    },
    {
      key: "attempts",
      header: "Attempts",
      render: (message: AdminEmailOutboxDto) => (
        <span className="text-sm">{message.attempts}</span>
      ),
    },
    {
      key: "error",
      header: "Last Error",
      render: (message: AdminEmailOutboxDto) =>
        message.lastError ? (
          <p className="max-w-[200px] truncate text-xs text-destructive" title={message.lastError}>
            {message.lastError}
          </p>
        ) : (
          <span className="text-xs text-muted-foreground">-</span>
        ),
    },
    {
      key: "actions",
      header: "Actions",
      className: "text-right",
      render: (message: AdminEmailOutboxDto) =>
        message.status !== "Sent" ? (
          <div className="flex justify-end">
            <Button
              variant="outline"
              size="sm"
              className="rounded-lg"
              onClick={() => setRetryMessage(message)}
            >
              <RefreshCw className="h-3.5 w-3.5" />
              Retry
            </Button>
          </div>
        ) : null,
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <section className="relative overflow-hidden rounded-2xl border border-border/60 bg-gradient-to-r from-blue-500/[0.04] via-card to-card p-6 shadow-sm">
        <div className="pointer-events-none absolute -right-16 -top-16 h-40 w-40 rounded-full bg-blue-500/5 blur-3xl" />
        <div className="relative flex items-start justify-between gap-4">
          <div>
            <div className="flex items-center gap-2">
              <SendToBack className="h-4 w-4 text-blue-600" />
              <p className="text-sm font-medium text-muted-foreground">Admin</p>
            </div>
            <h1 className="mt-2 text-2xl font-bold tracking-tight">
              Email Outbox
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Inspect queued email delivery and retry failed messages.
            </p>
          </div>
          <div className="flex items-center gap-2 rounded-xl bg-muted px-3 py-2 text-sm font-semibold">
            <Mail className="h-4 w-4" />
            {result?.total ?? 0}
          </div>
        </div>
      </section>

      {/* Search & Filter */}
      <div className="flex flex-col gap-3 rounded-2xl border border-border/60 bg-card p-4 shadow-sm md:flex-row">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="rounded-xl pl-9"
            placeholder="Filter recipient email"
            value={recipient}
            onChange={(event) => {
              setRecipient(event.target.value);
              setPage(1);
            }}
          />
        </div>
        <select
          className="h-10 rounded-xl border border-input bg-background px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          value={status}
          onChange={(event) => {
            setStatus(event.target.value);
            setPage(1);
          }}
        >
          <option value="">All statuses</option>
          <option value="Pending">Pending</option>
          <option value="Processing">Processing</option>
          <option value="Sent">Sent</option>
          <option value="Failed">Failed</option>
        </select>
      </div>

      {query.error ? (
        <InlineError
          message={getApiErrorMessage(query.error)}
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {/* Data Table */}
      {query.isLoading ? (
        <div className="space-y-4">
          <Skeleton className="h-10 w-full rounded-xl" />
          <Skeleton className="h-64 w-full rounded-2xl" />
        </div>
      ) : (
        <div className="space-y-4">
          <DataTable
            columns={columns}
            data={result?.items ?? []}
            keyExtractor={(message) => message.id}
            emptyMessage="No messages match the current filters."
          />

          {/* Pagination */}
          {result && result.total > result.pageSize ? (
            <div className="flex items-center justify-end gap-2 px-2">
              <Button
                variant="outline"
                size="sm"
                className="rounded-lg"
                disabled={page <= 1}
                onClick={() => setPage((value) => value - 1)}
              >
                Previous
              </Button>
              <span className="text-sm text-muted-foreground">Page {page}</span>
              <Button
                variant="outline"
                size="sm"
                className="rounded-lg"
                disabled={page * result.pageSize >= result.total}
                onClick={() => setPage((value) => value + 1)}
              >
                Next
              </Button>
            </div>
          ) : null}
        </div>
      )}

      <ConfirmDialog
        open={Boolean(retryMessage)}
        title="Retry email?"
        description={`Queue another delivery attempt for ${
          retryMessage?.recipient ?? "this recipient"
        }.`}
        confirmLabel="Retry email"
        isLoading={retryMutation.isPending}
        onClose={() => setRetryMessage(null)}
        onConfirm={() => retryMutation.mutate()}
      />
    </div>
  );
}

function statusTone(
  status: string
): "success" | "warning" | "danger" | "muted" {
  if (status === "Sent") return "success";
  if (status === "Failed") return "danger";
  if (status === "Pending") return "warning";
  return "muted";
}
