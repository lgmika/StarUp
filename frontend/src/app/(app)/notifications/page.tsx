"use client";

import { useEffect, useMemo, useState } from "react";
import { Archive, Bell, Check, CheckCheck, RefreshCw } from "lucide-react";
import { MockNotice } from "@/components/admin/mock-notice";
import { LoadingState } from "@/components/common/loading-state";
import { NotificationTypeBadge } from "@/components/notifications/notification-type-badge";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { cn } from "@/lib/utils";
import { notificationService } from "@/services";
import type { NotificationDto } from "@/types/notification";

type NotificationFilter = "all" | "unread" | "read";

const filters: Array<{ label: string; value: NotificationFilter }> = [
  { label: "All", value: "all" },
  { label: "Unread", value: "unread" },
  { label: "Read", value: "read" },
];

export default function NotificationsPage() {
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [filter, setFilter] = useState<NotificationFilter>("all");
  const [isLoading, setIsLoading] = useState(true);
  const [isMutating, setIsMutating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function loadNotifications() {
    setError(null);
    try {
      const [nextNotifications, nextUnreadCount] = await Promise.all([
        notificationService.listNotifications(),
        notificationService.getUnreadCount(),
      ]);
      setNotifications(nextNotifications);
      setUnreadCount(nextUnreadCount);
    } catch {
      setError("Could not load notifications from the mock service.");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadNotifications();
  }, []);

  const filteredNotifications = useMemo(() => {
    return notifications.filter((notification) => {
      if (filter === "unread") return !notification.readAt;
      if (filter === "read") return Boolean(notification.readAt);
      return true;
    });
  }, [filter, notifications]);

  async function runMutation(action: () => Promise<unknown>) {
    setIsMutating(true);
    setError(null);
    try {
      await action();
      await loadNotifications();
    } catch {
      setError("Notification action failed. Please try again.");
    } finally {
      setIsMutating(false);
    }
  }

  if (isLoading) return <LoadingState label="Loading notifications" />;

  return (
    <div className="space-y-5">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-start">
        <div>
          <h1 className="text-2xl font-semibold">Notifications</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Mock-backed notification center prepared for future backend notification endpoints.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" size="sm" onClick={() => void loadNotifications()} disabled={isMutating}>
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
          <Button
            size="sm"
            onClick={() => void runMutation(() => notificationService.markAllRead())}
            disabled={isMutating || unreadCount === 0}
          >
            <CheckCheck className="h-4 w-4" />
            Mark all read
          </Button>
        </div>
      </div>

      <MockNotice label="Notifications" />

      <div className="grid gap-4 md:grid-cols-3">
        <Panel>
          <PanelBody>
            <Bell className="h-5 w-5 text-muted-foreground" />
            <p className="mt-4 text-2xl font-semibold">{notifications.length}</p>
            <p className="mt-1 text-sm text-muted-foreground">Visible notifications</p>
          </PanelBody>
        </Panel>
        <Panel>
          <PanelBody>
            <Check className="h-5 w-5 text-muted-foreground" />
            <p className="mt-4 text-2xl font-semibold">{unreadCount}</p>
            <p className="mt-1 text-sm text-muted-foreground">Unread</p>
          </PanelBody>
        </Panel>
        <Panel>
          <PanelBody>
            <Archive className="h-5 w-5 text-muted-foreground" />
            <p className="mt-4 text-2xl font-semibold">{notifications.length - unreadCount}</p>
            <p className="mt-1 text-sm text-muted-foreground">Read</p>
          </PanelBody>
        </Panel>
      </div>

      <Panel>
        <PanelHeader className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <PanelTitle>Inbox</PanelTitle>
          <div className="flex flex-wrap gap-2">
            {filters.map((item) => (
              <Button
                key={item.value}
                variant={filter === item.value ? "primary" : "outline"}
                size="sm"
                onClick={() => setFilter(item.value)}
              >
                {item.label}
              </Button>
            ))}
          </div>
        </PanelHeader>
        <PanelBody className="space-y-3">
          {error ? (
            <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div>
          ) : null}

          {filteredNotifications.length === 0 ? (
            <div className="rounded-md border border-dashed border-border p-8 text-center">
              <p className="text-sm font-medium">No notifications found</p>
              <p className="mt-1 text-sm text-muted-foreground">Change the filter or refresh the mock inbox.</p>
            </div>
          ) : (
            filteredNotifications.map((notification) => (
              <article
                key={notification.id}
                className={cn(
                  "rounded-md border border-border p-4 transition-colors",
                  notification.readAt ? "bg-card" : "bg-primary/5"
                )}
              >
                <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                  <div className="min-w-0 space-y-2">
                    <div className="flex flex-wrap items-center gap-2">
                      <NotificationTypeBadge type={notification.type} />
                      <Badge tone={notification.readAt ? "muted" : "success"}>
                        {notification.readAt ? "Read" : "Unread"}
                      </Badge>
                      {notification.resourceType ? <Badge tone="default">{notification.resourceType}</Badge> : null}
                    </div>
                    <div>
                      <h2 className="text-sm font-semibold">{notification.title}</h2>
                      <p className="mt-1 text-sm text-muted-foreground">{notification.message}</p>
                    </div>
                    <p className="text-xs text-muted-foreground">{formatDate(notification.createdAt)}</p>
                  </div>
                  <div className="flex shrink-0 flex-wrap gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => void runMutation(() => notificationService.markRead(notification.id))}
                      disabled={isMutating || Boolean(notification.readAt)}
                    >
                      <Check className="h-4 w-4" />
                      Mark read
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => void runMutation(() => notificationService.delete(notification.id))}
                      disabled={isMutating}
                    >
                      <Archive className="h-4 w-4" />
                      Archive
                    </Button>
                  </div>
                </div>
              </article>
            ))
          )}
        </PanelBody>
      </Panel>
    </div>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
