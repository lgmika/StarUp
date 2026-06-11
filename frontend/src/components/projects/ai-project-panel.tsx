"use client";

import { useEffect, useState } from "react";
import { Bot, CheckCircle2, Loader2, Sparkles, TriangleAlert } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { projectService } from "@/services";
import type { AIRecommendationDto, AIReviewDto } from "@/types/ai";

export function AiProjectPanel({ projectId }: { projectId: string }) {
  const [recommendations, setRecommendations] = useState<AIRecommendationDto[]>([]);
  const [review, setReview] = useState<AIReviewDto | null>(null);
  const [isSuggesting, setIsSuggesting] = useState(false);
  const [isReviewing, setIsReviewing] = useState(false);

  async function generateSuggestions() {
    setIsSuggesting(true);
    try {
      setRecommendations(await projectService.createAiSuggestions(projectId));
      toast.success("AI suggestions generated.");
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    } finally {
      setIsSuggesting(false);
    }
  }

  async function generateReview() {
    setIsReviewing(true);
    try {
      setReview(await projectService.createAiReview(projectId));
      toast.success("AI review generated.");
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    } finally {
      setIsReviewing(false);
    }
  }

  async function applyRecommendation(recommendationId: string) {
    try {
      await projectService.applyAiRecommendation(recommendationId);
      setRecommendations((current) =>
        current.map((item) => (item.id === recommendationId ? { ...item, isApplied: true } : item))
      );
      toast.success("Recommendation marked as applied.");
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    }
  }

  useEffect(() => {
    async function loadLatestReview() {
      try {
        setReview(await projectService.getLatestAiReview(projectId));
      } catch {
        setReview(null);
      }
    }

    void loadLatestReview();
  }, [projectId]);

  return (
    <div className="space-y-4">
      <Panel>
        <PanelHeader className="flex flex-row items-center justify-between gap-3">
          <PanelTitle>AI support</PanelTitle>
          <Badge tone="muted">MockAIService</Badge>
        </PanelHeader>
        <PanelBody className="flex flex-wrap gap-2">
          <Button type="button" variant="outline" disabled={isSuggesting} onClick={() => void generateSuggestions()}>
            {isSuggesting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />}
            Suggestions
          </Button>
          <Button type="button" variant="outline" disabled={isReviewing} onClick={() => void generateReview()}>
            {isReviewing ? <Loader2 className="h-4 w-4 animate-spin" /> : <Bot className="h-4 w-4" />}
            Review
          </Button>
        </PanelBody>
      </Panel>

      {review ? (
        <Panel>
          <PanelHeader>
            <PanelTitle>AI review score</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-4">
            <div className="flex items-end gap-2">
              <span className="text-4xl font-semibold">{review.qualityScore}</span>
              <span className="pb-1 text-sm text-muted-foreground">/ 100</span>
            </div>
            <p className="text-sm leading-6 text-muted-foreground">{review.summary}</p>
            <Checklist title="Missing information" items={review.missingInformation} />
            <Checklist title="Risk flags" items={review.riskFlags} risk />
            <Checklist title="Suggestions" items={review.suggestions} />
          </PanelBody>
        </Panel>
      ) : null}

      {recommendations.length ? (
        <Panel>
          <PanelHeader>
            <PanelTitle>AI suggestions</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-3">
            {recommendations.map((recommendation) => (
              <div key={recommendation.id} className="rounded-md border border-border p-3">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="text-sm font-semibold">{recommendation.title}</p>
                    <p className="mt-1 text-xs text-muted-foreground">Target: {recommendation.targetField}</p>
                  </div>
                  <Badge tone={recommendation.isApplied ? "success" : "muted"}>
                    {recommendation.isApplied ? "Applied" : "New"}
                  </Badge>
                </div>
                <p className="mt-3 text-sm leading-6 text-muted-foreground">{recommendation.content}</p>
                <Button
                  className="mt-3"
                  size="sm"
                  variant="outline"
                  disabled={recommendation.isApplied}
                  onClick={() => void applyRecommendation(recommendation.id)}
                >
                  <CheckCircle2 className="h-4 w-4" />
                  Mark applied
                </Button>
              </div>
            ))}
          </PanelBody>
        </Panel>
      ) : null}
    </div>
  );
}

function Checklist({ title, items, risk = false }: { title: string; items: string[]; risk?: boolean }) {
  if (!items.length) return null;

  return (
    <div>
      <p className="text-sm font-medium">{title}</p>
      <div className="mt-2 space-y-2">
        {items.map((item) => (
          <div key={item} className="flex gap-2 text-sm text-muted-foreground">
            {risk ? <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0 text-amber-600" /> : <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-emerald-600" />}
            <span>{item}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
