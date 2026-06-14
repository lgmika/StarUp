"use client";

import { useEffect, useState } from "react";
import { Sparkles, X } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { PageHeader } from "@/components/workspace/page-header";
import { StatusBadge } from "@/components/workspace/status-badge";
import { recommendationService } from "@/services/recommendation-service";
import type { RecommendationItem } from "@/types/workspace";

export default function RecommendationsPage() {
  const [items, setItems] = useState<RecommendationItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadRecommendations() {
      try {
        const [projects, members] = await Promise.all([
          recommendationService.projects(),
          recommendationService.members(),
        ]);
        setItems([
          ...projects.items.map((item) => ({
            id: item.recommendationId,
            title: item.title,
            category: "Project" as const,
            score: item.score,
            reason: item.breakdown[0]?.explanation ?? item.summary,
            status: item.stage,
          })),
          ...members.items.map((item) => ({
            id: item.recommendationId,
            title: item.fullName,
            category: "Teammate" as const,
            score: item.score,
            reason: item.breakdown[0]?.explanation ?? item.headline,
            status: item.projectTitle,
          })),
        ]);
      } catch (loadError) {
        setError(getApiErrorMessage(loadError));
      } finally {
        setIsLoading(false);
      }
    }

    void loadRecommendations();
  }, []);

  async function dismiss(id: string) {
    try {
      await recommendationService.dismiss(id);
      setItems((current) => current.filter((item) => item.id !== id));
    } catch (dismissError) {
      setError(getApiErrorMessage(dismissError));
    }
  }

  if (isLoading) return <LoadingState label="Đang tải đề xuất" />;

  return (
    <div className="space-y-5">
      <PageHeader title="Recommendations" description="Dự án, cộng sự và nhà đầu tư được gợi ý dựa trên hồ sơ và hoạt động." />
      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}
      <div className="grid gap-4 lg:grid-cols-3">
        {items.map((item) => (
          <Panel key={item.id}>
            <PanelBody>
              <div className="flex items-start justify-between gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10 text-primary">
                  <Sparkles className="h-4 w-4" />
                </div>
                <Button variant="ghost" size="icon" aria-label={`Ẩn đề xuất ${item.title}`} onClick={() => void dismiss(item.id)}>
                  <X className="h-4 w-4" />
                </Button>
              </div>
              <div className="mt-4 flex items-center justify-between gap-3">
                <h2 className="font-semibold">{item.title}</h2>
                <StatusBadge value={item.category} />
              </div>
              <p className="mt-3 text-3xl font-semibold text-primary">{item.score}%</p>
              <p className="mt-2 text-sm text-muted-foreground">{item.reason}</p>
              <div className="mt-4">
                <StatusBadge value={item.status} />
              </div>
            </PanelBody>
          </Panel>
        ))}
      </div>
      {!error && items.length === 0 ? (
        <div className="rounded-md border border-dashed border-border p-8 text-center text-sm text-muted-foreground">
          Chưa có recommendation phù hợp.
        </div>
      ) : null}
    </div>
  );
}
