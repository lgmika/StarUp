"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useState } from "react";
import { Search } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { PageHeader } from "@/components/workspace/page-header";
import { StatusBadge } from "@/components/workspace/status-badge";
import { searchService } from "@/services/search-service";
import { ProjectStage, ProjectStatus } from "@/types/enums";
import type { SearchResultItem } from "@/types/workspace";

const tabs = ["Projects", "Members", "Investors", "Suggestions"];

export default function SearchPage() {
  const [keyword, setKeyword] = useState("");
  const [activeTab, setActiveTab] = useState("Projects");
  const [results, setResults] = useState<SearchResultItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [status, setStatus] = useState("");
  const [stage, setStage] = useState("");
  const [error, setError] = useState<string | null>(null);

  const loadResults = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      if (activeTab === "Projects") {
        const response = await searchService.projects({
          keyword: keyword || undefined,
          status: status ? (status as ProjectStatus) : undefined,
          stage: stage ? (stage as ProjectStage) : undefined,
        });
        setResults(response.items.map((item) => ({
          id: item.id,
          title: item.title,
          type: "Project",
          description: item.summary,
          status: item.status,
        })));
      } else if (activeTab === "Members") {
        const response = await searchService.members({ keyword: keyword || undefined });
        setResults(response.items.map((item) => ({
          id: item.userId,
          title: item.fullName,
          type: "Member",
          description: [item.headline, item.location, item.skills.join(", ")].filter(Boolean).join(" · "),
          status: "Member",
        })));
      } else if (activeTab === "Investors") {
        const response = await searchService.investors({ keyword: keyword || undefined });
        setResults(response.items.map((item) => ({
          id: item.userId,
          title: item.displayName,
          type: "Investor",
          description: [item.organizationName, item.investmentFocus].filter(Boolean).join(" · "),
          status: "Investor",
        })));
      } else {
        const response = await searchService.suggestions(keyword || undefined);
        setResults(response.items.map((item, index) => ({
          id: item.id ?? `suggestion-${index}`,
          title: item.label,
          type: "Suggestion",
          description: item.description ?? item.type,
          status: item.type,
        })));
      }
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
      setResults([]);
    } finally {
      setIsLoading(false);
    }
  }, [activeTab, keyword, stage, status]);

  useEffect(() => {
    void loadResults();
  }, [loadResults]);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void loadResults();
  }

  const visibleResults = results.filter((result) => {
    if (activeTab === "Projects") return result.type === "Project";
    if (activeTab === "Members") return result.type === "Member";
    if (activeTab === "Investors") return result.type === "Investor";
    return result.type === "Suggestion";
  });

  return (
    <div className="space-y-5">
      <PageHeader title="Tìm kiếm nâng cao" description="Tìm dự án, thành viên, nhà đầu tư và gợi ý AI theo tab và bộ lọc." />
      <Panel>
        <PanelBody>
          <form className="grid gap-3 lg:grid-cols-[1fr_180px_180px_auto]" onSubmit={handleSubmit}>
            <Input value={keyword} onChange={(event) => setKeyword(event.target.value)} placeholder="Từ khóa, kỹ năng, vị trí..." />
            <select className="h-10 rounded-md border border-input bg-background px-3 text-sm" value={status} onChange={(event) => setStatus(event.target.value)}>
              <option value="">Trạng thái</option>
              {Object.values(ProjectStatus).map((item) => <option key={item} value={item}>{item}</option>)}
            </select>
            <select className="h-10 rounded-md border border-input bg-background px-3 text-sm" value={stage} onChange={(event) => setStage(event.target.value)}>
              <option value="">Stage</option>
              {Object.values(ProjectStage).map((item) => <option key={item} value={item}>{item}</option>)}
            </select>
            <Button type="submit">
              <Search className="h-4 w-4" />
              Tìm
            </Button>
          </form>
        </PanelBody>
      </Panel>
      <Panel>
        <PanelHeader>
          <div className="flex flex-wrap gap-2">
            {tabs.map((tab) => (
              <Button key={tab} size="sm" variant={activeTab === tab ? "primary" : "outline"} onClick={() => setActiveTab(tab)}>
                {tab}
              </Button>
            ))}
          </div>
        </PanelHeader>
        <PanelBody className="space-y-3">
          {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}
          {isLoading ? <LoadingState label="Đang tìm kiếm" /> : null}
          {!isLoading && !error && visibleResults.map((result) => (
            <div key={result.id} className="rounded-md border border-border p-4">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <PanelTitle>{result.type === "Member" ? <Link className="hover:text-primary" href={`/members/${result.id}`}>{result.title}</Link> : result.title}</PanelTitle>
                <StatusBadge value={result.status} />
              </div>
              <p className="mt-2 text-sm text-muted-foreground">{result.description}</p>
            </div>
          ))}
          {!isLoading && !error && visibleResults.length === 0 ? (
            <div className="rounded-md border border-dashed border-border p-8 text-center text-sm text-muted-foreground">
              Không có kết quả phù hợp.
            </div>
          ) : null}
        </PanelBody>
      </Panel>
    </div>
  );
}
