import Link from "next/link";
import { ArrowRight, Bookmark, UsersRound, Wallet } from "lucide-react";
import type { LandingProject } from "@/types/project";

const fieldColors: Record<string, string> = {
  GreenTech: "bg-emerald-100 text-emerald-700",
  EdTech: "bg-blue-100 text-blue-700",
  FinTech: "bg-violet-100 text-violet-700",
  AgriTech: "bg-lime-100 text-lime-700",
  HealthTech: "bg-rose-100 text-rose-700",
  Logistics: "bg-orange-100 text-orange-700",
  AI: "bg-amber-100 text-amber-700",
  "E-commerce": "bg-teal-100 text-teal-700",
};

export function ProjectCard({ project }: { project: LandingProject }) {
  const fieldColor = fieldColors[project.field] ?? "bg-primary/10 text-primary";

  return (
    <article className="group flex h-full flex-col rounded-2xl border border-border/60 bg-card p-6 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-primary/20 hover:shadow-lg">
      <div className="flex items-start justify-between gap-3">
        <div>
          <span className={`inline-flex items-center rounded-lg px-2.5 py-1 text-xs font-semibold ${fieldColor}`}>
            {project.field}
          </span>
          <h3 className="mt-3 text-lg font-bold">{project.title}</h3>
        </div>
        <button
          type="button"
          className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl border border-border text-muted-foreground transition-all hover:border-primary/30 hover:bg-primary/5 hover:text-primary"
          aria-label={`Save project ${project.title}`}
        >
          <Bookmark className="h-4 w-4" />
        </button>
      </div>

      <p className="mt-3 flex-1 text-sm leading-6 text-muted-foreground">{project.summary}</p>

      <div className="mt-4 flex flex-wrap gap-2">
        <span className="inline-flex items-center rounded-lg bg-muted px-2.5 py-1 text-xs font-medium text-muted-foreground">
          {project.stage}
        </span>
        <span className={`inline-flex items-center rounded-lg px-2.5 py-1 text-xs font-medium ${project.seekingInvestor ? "bg-amber-100 text-amber-700" : "bg-emerald-100 text-emerald-700"}`}>
          {project.status}
        </span>
      </div>

      <div className="mt-4 flex flex-wrap gap-1.5">
        {project.roles.slice(0, 3).map((role) => (
          <span
            key={role}
            className="rounded-full border border-border/60 bg-muted/50 px-2.5 py-1 text-xs font-medium text-muted-foreground"
          >
            {role}
          </span>
        ))}
      </div>

      <div className="mt-5 flex items-center justify-between border-t border-border/60 pt-4">
        <span className="inline-flex items-center gap-2 text-sm text-muted-foreground">
          <UsersRound className="h-4 w-4" />
          {project.memberCount} members
        </span>
        {project.seekingInvestor ? (
          <span className="inline-flex items-center gap-1 text-xs font-semibold text-amber-600">
            <Wallet className="h-3.5 w-3.5" />
            Seeking investor
          </span>
        ) : null}
      </div>

      <Link
        href={project.href}
        className="mt-4 inline-flex h-10 items-center justify-center gap-2 rounded-xl border border-border/60 text-sm font-semibold transition-all hover:border-primary/30 hover:bg-primary/5 hover:text-primary"
      >
        View details
        <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
      </Link>
    </article>
  );
}
