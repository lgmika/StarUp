import Link from "next/link";
import { Bookmark, ExternalLink, UsersRound } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import type { LandingProject } from "@/types/project";

export function ProjectCard({ project }: { project: LandingProject }) {
  return (
    <article className="flex h-full flex-col rounded-2xl border border-border bg-card p-5 shadow-sm transition-transform hover:-translate-y-1 hover:shadow-md">
      <div className="flex items-start justify-between gap-3">
        <div>
          <Badge tone="default">{project.field}</Badge>
          <h3 className="mt-4 text-lg font-semibold">{project.title}</h3>
        </div>
        <button
          type="button"
          className="flex h-9 w-9 items-center justify-center rounded-md border border-border text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
          aria-label={`Lưu dự án ${project.title}`}
        >
          <Bookmark className="h-4 w-4" />
        </button>
      </div>

      <p className="mt-3 flex-1 text-sm leading-6 text-muted-foreground">{project.summary}</p>

      <div className="mt-4 flex flex-wrap gap-2">
        <Badge tone="muted">{project.stage}</Badge>
        <Badge tone={project.seekingInvestor ? "warning" : "success"}>{project.status}</Badge>
      </div>

      <div className="mt-4 flex flex-wrap gap-2">
        {project.roles.slice(0, 3).map((role) => (
          <span key={role} className="rounded-full bg-muted px-3 py-1 text-xs font-medium text-muted-foreground">
            {role}
          </span>
        ))}
      </div>

      <div className="mt-5 flex items-center justify-between border-t border-border pt-4">
        <span className="inline-flex items-center gap-2 text-sm text-muted-foreground">
          <UsersRound className="h-4 w-4" />
          {project.memberCount} thành viên
        </span>
        {project.seekingInvestor ? <Badge tone="warning">Investor</Badge> : null}
      </div>

      <Link
        href={project.href}
        className="mt-5 inline-flex h-10 items-center justify-center gap-2 rounded-md border border-border text-sm font-medium transition-colors hover:bg-accent"
      >
        Xem chi tiết
        <ExternalLink className="h-4 w-4" />
      </Link>
    </article>
  );
}
