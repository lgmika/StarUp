import Link from "next/link";
import { ArrowRight, CalendarDays, Search, UsersRound } from "lucide-react";
import { ProjectStageBadge, ProjectStatusBadge, ProjectVisibilityBadge } from "@/components/projects/project-badges";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody } from "@/components/ui/panel";
import type { ProjectSummaryDto } from "@/types/project";

export function ProjectCard({ project }: { project: ProjectSummaryDto }) {
  return (
    <Panel className="h-full transition-colors hover:bg-accent/50">
      <PanelBody className="flex h-full flex-col gap-4">
        <div className="flex flex-wrap items-center gap-2">
          <ProjectStatusBadge status={project.status} />
          <ProjectStageBadge stage={project.stage} />
          <ProjectVisibilityBadge visibility={project.visibility} />
          {project.isRecruiting ? (
            <Badge tone="success">
              <UsersRound className="mr-1 h-3 w-3" />
              Recruiting
            </Badge>
          ) : null}
        </div>

        <div className="min-w-0">
          <h2 className="line-clamp-2 text-base font-semibold">{project.title}</h2>
          <p className="mt-2 line-clamp-3 text-sm leading-6 text-muted-foreground">{project.summary}</p>
        </div>

        <div className="mt-auto flex flex-wrap items-center justify-between gap-3 border-t border-border pt-4 text-xs text-muted-foreground">
          <span className="inline-flex min-w-0 items-center gap-1">
            <Search className="h-3.5 w-3.5" />
            <span className="truncate">{project.slug}</span>
          </span>
          <span className="inline-flex items-center gap-1">
            <CalendarDays className="h-3.5 w-3.5" />
            {new Date(project.createdAt).toLocaleDateString()}
          </span>
        </div>
        <Link
          className="inline-flex h-9 items-center justify-center gap-2 rounded-md border border-border bg-background px-3 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground"
          href={`/projects/${project.id}`}
        >
          View detail
          <ArrowRight className="h-4 w-4" />
        </Link>
      </PanelBody>
    </Panel>
  );
}
