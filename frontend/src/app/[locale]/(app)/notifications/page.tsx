"use client";

import Link from "next/link";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Archive, Bell, Check, CheckCheck, ExternalLink, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { LoadingState } from "@/components/common/loading-state";
import { NotificationTypeBadge } from "@/components/notifications/notification-type-badge";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { getApiErrorMessage } from "@/lib/api";
import { queryKeys } from "@/lib/query-keys";
import { cn } from "@/lib/utils";
import { notificationService } from "@/services";
import { NotificationType } from "@/types/enums";

export default function NotificationsPage() {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState("all");
  const [type, setType] = useState("");
  const [page, setPage] = useState(1);
  const notificationsQuery = useQuery({ queryKey: [...queryKeys.notifications, { status, type, page }], queryFn: () => notificationService.list({ status, type: type || undefined, page, pageSize: 20 }) });
  const refresh = () => queryClient.invalidateQueries({ queryKey: queryKeys.notifications });
  const mutation = useMutation({
    mutationFn: async ({ action, id }: { action: "read" | "delete" | "read-all"; id?: string }) => {
      if (action === "read" && id) return notificationService.markRead(id);
      if (action === "delete" && id) return notificationService.delete(id);
      return notificationService.markAllRead();
    },
    onSuccess: () => void refresh(),
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  if (notificationsQuery.isLoading) return <LoadingState label="Loading notifications" />;
  const result = notificationsQuery.data;
  const pages = result ? Math.max(1, Math.ceil(result.total / result.pageSize)) : 1;

  return <div className="space-y-5">
    <div className="flex flex-col justify-between gap-4 md:flex-row md:items-start"><div><h1 className="text-2xl font-semibold">Notifications</h1><p className="mt-2 text-sm text-muted-foreground">Realtime inbox with server-side filters and direct action links.</p></div><div className="flex gap-2"><Button variant="outline" size="sm" onClick={() => void refresh()}><RefreshCw className="h-4 w-4" />Refresh</Button><Button size="sm" disabled={mutation.isPending || !result?.unreadCount} onClick={() => mutation.mutate({ action: "read-all" })}><CheckCheck className="h-4 w-4" />Mark all read</Button></div></div>
    <div className="grid gap-4 md:grid-cols-3"><Metric icon={Bell} label="Total" value={result?.total ?? 0} /><Metric icon={Check} label="Unread" value={result?.unreadCount ?? 0} /><Metric icon={Archive} label="Read" value={(result?.total ?? 0) - (result?.unreadCount ?? 0)} /></div>
    <Panel><PanelHeader className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between"><PanelTitle>Inbox</PanelTitle><div className="flex flex-wrap gap-2"><select className="h-9 rounded-md border border-input bg-background px-3 text-sm" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1); }}><option value="all">All</option><option value="unread">Unread</option><option value="read">Read</option></select><select className="h-9 rounded-md border border-input bg-background px-3 text-sm" value={type} onChange={(event) => { setType(event.target.value); setPage(1); }}><option value="">All types</option>{Object.values(NotificationType).map((item) => <option key={item} value={item}>{item}</option>)}</select></div></PanelHeader><PanelBody className="space-y-3">
      {notificationsQuery.error ? <p className="rounded-md bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(notificationsQuery.error)}</p> : null}
      {!result?.items.length ? <EmptyState icon={Bell} title="No notifications" description="No notifications match the current filters." /> : result.items.map((notification) => <article key={notification.id} className={cn("rounded-md border border-border p-4", notification.isRead ? "bg-card" : "bg-primary/5")}><div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between"><div className="space-y-2"><div className="flex flex-wrap gap-2"><NotificationTypeBadge type={notification.type} /><Badge tone={notification.isRead ? "muted" : "success"}>{notification.isRead ? "Read" : "Unread"}</Badge></div><div><h2 className="text-sm font-semibold">{notification.title}</h2><p className="mt-1 text-sm text-muted-foreground">{notification.message}</p></div><p className="text-xs text-muted-foreground">{new Date(notification.createdAt).toLocaleString()}</p></div><div className="flex gap-2">{notification.actionUrl ? <Link href={notification.actionUrl} className="inline-flex h-8 items-center gap-2 rounded-md border border-border px-3 text-xs font-medium hover:bg-accent"><ExternalLink className="h-3.5 w-3.5" />Open</Link> : null}<Button size="sm" variant="outline" disabled={mutation.isPending || notification.isRead} onClick={() => mutation.mutate({ action: "read", id: notification.id })}><Check className="h-4 w-4" />Read</Button><Button size="icon" variant="ghost" aria-label="Archive notification" disabled={mutation.isPending} onClick={() => mutation.mutate({ action: "delete", id: notification.id })}><Archive className="h-4 w-4" /></Button></div></div></article>)}
      <div className="flex items-center justify-between pt-2"><p className="text-xs text-muted-foreground">Page {page} of {pages}</p><div className="flex gap-2"><Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Previous</Button><Button size="sm" variant="outline" disabled={page >= pages} onClick={() => setPage((value) => value + 1)}>Next</Button></div></div>
    </PanelBody></Panel>
  </div>;
}

function Metric({ icon: Icon, label, value }: { icon: typeof Bell; label: string; value: number }) { return <Panel><PanelBody><Icon className="h-5 w-5 text-muted-foreground" /><p className="mt-4 text-2xl font-semibold">{value}</p><p className="mt-1 text-sm text-muted-foreground">{label}</p></PanelBody></Panel>; }
