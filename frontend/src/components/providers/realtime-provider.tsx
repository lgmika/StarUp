"use client";

import { useEffect } from "react";
import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { getAccessToken } from "@/lib/auth";
import { queryKeys } from "@/lib/query-keys";
import { REALTIME_HUB_URL } from "@/lib/config";
import { useAuthStore } from "@/stores/auth-store";

const eventQueries: Record<string, readonly unknown[]> = {
  "notification.created": queryKeys.notifications,
  "notification.read": queryKeys.notifications,
  "notification.deleted": queryKeys.notifications,
  "notifications.readAll": queryKeys.notifications,
  "message.created": queryKeys.conversations,
  "message.deleted": queryKeys.conversations,
  "message.read": queryKeys.conversations,
  "conversation.created": queryKeys.conversations,
  "project.status.changed": queryKeys.projects,
  "application.statusChanged": queryKeys.applications,
  "application.status.changed": queryKeys.applications,
  "investorInterest.changed": queryKeys.investorInterests,
  "interview.changed": queryKeys.interviews,
  "report.changed": queryKeys.reports,
  "nda.agreement.accepted": queryKeys.nda,
  "billing.subscription.changed": queryKeys.billing,
};

export function RealtimeProvider({ children }: { children: React.ReactNode }) {
  const queryClient = useQueryClient();
  const userId = useAuthStore((state) => state.user?.id);

  useEffect(() => {
    if (!userId || !getAccessToken()) return;

    const connection = new HubConnectionBuilder()
      .withUrl(REALTIME_HUB_URL, { accessTokenFactory: () => getAccessToken() ?? "" })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(LogLevel.Warning)
      .build();

    for (const [eventName, queryKey] of Object.entries(eventQueries)) {
      connection.on(eventName, () => {
        void queryClient.invalidateQueries({ queryKey });
      });
    }

    void connection.start().catch(() => undefined);

    return () => {
      if (connection.state !== HubConnectionState.Disconnected) {
        void connection.stop();
      }
    };
  }, [queryClient, userId]);

  return children;
}
