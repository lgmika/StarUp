"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { ExternalLink, UsersRound } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { PageHeader } from "@/components/workspace/page-header";
import { StatusBadge } from "@/components/workspace/status-badge";
import { projectService } from "@/services/project-service";
import type { ProjectSummaryDto } from "@/types/project";

export default function JoinedProjectsPage() {
  const [projects, setProjects] = useState<ProjectSummaryDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadProjects() {
      try {
        setProjects(await projectService.listJoinedProjects());
      } catch (loadError) {
        setError(getApiErrorMessage(loadError));
      } finally {
        setIsLoading(false);
      }
    }

    void loadProjects();
  }, []);

  if (isLoading) return <LoadingState label="Đang tải dự án tham gia" />;

  return (
    <div className="space-y-5">
      <PageHeader
        title="Joined Projects"
        description="Theo dõi các dự án bạn đang tham gia, vai trò và việc tiếp theo cần xử lý."
        actions={
          <Link href="/projects" className="inline-flex h-9 items-center gap-2 rounded-md border border-border px-3 text-sm font-medium hover:bg-accent">
            <UsersRound className="h-4 w-4" />
            Tìm thêm dự án
          </Link>
        }
      />
      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}
      <Panel>
        <PanelHeader>
          <PanelTitle>Dự án đang tham gia</PanelTitle>
        </PanelHeader>
        <PanelBody className="overflow-x-auto">
          <table className="w-full min-w-[760px] text-left text-sm">
            <thead className="border-b border-border text-xs text-muted-foreground">
              <tr>
                <th className="py-2 font-medium">Dự án</th>
                <th className="py-2 font-medium">Vai trò</th>
                <th className="py-2 font-medium">Stage</th>
                <th className="py-2 font-medium">Trạng thái</th>
                <th className="py-2 font-medium">Việc tiếp theo</th>
                <th className="py-2 text-right font-medium">Action</th>
              </tr>
            </thead>
            <tbody>
              {projects.map((project) => (
                <tr key={project.id} className="border-b border-border">
                  <td className="py-3 font-medium">{project.title}</td>
                  <td className="py-3 text-muted-foreground">Member</td>
                  <td className="py-3">{project.stage}</td>
                  <td className="py-3"><StatusBadge value={project.status} /></td>
                  <td className="py-3 text-muted-foreground">{project.isRecruiting ? "Review open roles" : "Follow project updates"}</td>
                  <td className="py-3 text-right">
                    <Link href={`/projects/${project.id}`} className="inline-flex h-8 items-center gap-2 rounded-md border border-border px-3 text-xs font-medium hover:bg-accent">
                      <ExternalLink className="h-4 w-4" />
                      Mở
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!error && projects.length === 0 ? <p className="py-8 text-center text-sm text-muted-foreground">Bạn chưa tham gia dự án nào.</p> : null}
        </PanelBody>
      </Panel>
    </div>
  );
}
