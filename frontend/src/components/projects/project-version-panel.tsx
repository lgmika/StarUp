"use client";

import { useEffect, useState } from "react";
import { Clock3, Loader2 } from "lucide-react";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { projectService } from "@/services";
import type { ProjectVersionDto } from "@/types/project";

export function ProjectVersionPanel({ projectId }: { projectId: string }) {
  const [versions, setVersions] = useState<ProjectVersionDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadVersions() {
      setIsLoading(true);
      setError(null);
      try {
        setVersions(await projectService.getProjectVersions(projectId));
      } catch (loadError) {
        setError(getApiErrorMessage(loadError));
      } finally {
        setIsLoading(false);
      }
    }

    void loadVersions();
  }, [projectId]);

  return (
    <Panel>
      <PanelHeader>
        <PanelTitle>Version history</PanelTitle>
      </PanelHeader>
      <PanelBody>
        {isLoading ? (
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading versions
          </div>
        ) : null}
        {error ? <p className="text-sm text-destructive">{error}</p> : null}
        {!isLoading && !error && versions.length === 0 ? (
          <p className="text-sm text-muted-foreground">No versions returned yet.</p>
        ) : null}
        <div className="space-y-3">
          {versions.map((version) => (
            <div key={version.id} className="flex gap-3 rounded-md border border-border p-3">
              <div className="mt-0.5 text-muted-foreground">
                <Clock3 className="h-4 w-4" />
              </div>
              <div>
                <p className="text-sm font-semibold">Version {version.versionNumber}</p>
                <p className="mt-1 text-sm text-muted-foreground">{version.changeReason}</p>
                <p className="mt-1 text-xs text-muted-foreground">{new Date(version.createdAt).toLocaleString()}</p>
              </div>
            </div>
          ))}
        </div>
      </PanelBody>
    </Panel>
  );
}
