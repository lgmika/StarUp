"use client";

import { useEffect, useState } from "react";
import { Activity, Filter } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { PageHeader } from "@/components/workspace/page-header";
import { StatusBadge } from "@/components/workspace/status-badge";
import { activityService } from "@/services/activity-service";
import type { ActivityDto } from "@/types/activity";

export default function FeedPage() {
  const [items, setItems] = useState<ActivityDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadFeed() {
      try {
        const response = await activityService.getFeed();
        setItems(response.items);
      } catch (loadError) {
        setError(getApiErrorMessage(loadError));
      } finally {
        setIsLoading(false);
      }
    }

    void loadFeed();
  }, []);

  if (isLoading) return <LoadingState label="Đang tải activity feed" />;

  return (
    <div className="space-y-5">
      <PageHeader
        title="Activity Feed"
        description="Luồng hoạt động thật từ backend cho dự án, ứng tuyển, NDA và kiểm duyệt."
        actions={
          <Button variant="outline" size="sm">
            <Filter className="h-4 w-4" />
            Bộ lọc
          </Button>
        }
      />
      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}
      <div className="space-y-3">
        {items.map((item) => (
          <Panel key={item.id}>
            <PanelBody className="flex gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-muted">
                <Activity className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <p className="text-sm font-medium">{item.actorName ?? "System"}</p>
                  <StatusBadge value={item.type} />
                </div>
                <p className="mt-1 text-sm text-muted-foreground">
                  {item.title} <span className="font-medium text-foreground">{item.projectTitle}</span>
                </p>
                {item.message ? <p className="mt-1 text-sm text-muted-foreground">{item.message}</p> : null}
                <p className="mt-2 text-xs text-muted-foreground">{item.createdAt}</p>
              </div>
            </PanelBody>
          </Panel>
        ))}
        {!error && items.length === 0 ? (
          <div className="rounded-md border border-dashed border-border p-8 text-center text-sm text-muted-foreground">
            Chưa có hoạt động nào.
          </div>
        ) : null}
      </div>
    </div>
  );
}
