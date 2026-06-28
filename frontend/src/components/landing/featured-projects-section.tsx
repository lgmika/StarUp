import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { ProjectCard } from "@/components/landing/project-card";
import { featuredProjects } from "@/lib/landing-data";

export function FeaturedProjectsSection() {
  return (
    <section className="bg-background py-16 sm:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col justify-between gap-4 md:flex-row md:items-end">
          <div>
            <span className="inline-flex items-center rounded-full bg-primary/10 px-3 py-1 text-xs font-semibold text-primary">
              Featured
            </span>
            <h2 className="mt-4 text-3xl font-bold tracking-tight sm:text-4xl">
              Dự án nổi bật
            </h2>
            <p className="mt-3 max-w-2xl text-base leading-7 text-muted-foreground">
              Khám phá những dự án đang tìm kiếm cộng sự và cơ hội hợp tác.
            </p>
          </div>
          <Link
            href="/projects"
            className="group inline-flex h-11 items-center gap-2 rounded-xl border border-border bg-background px-5 text-sm font-semibold transition-all hover:-translate-y-0.5 hover:shadow-md"
          >
            Xem tất cả dự án
            <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
          </Link>
        </div>

        <div className="mt-10 grid gap-6 md:grid-cols-2 xl:grid-cols-3">
          {featuredProjects.map((project) => (
            <ProjectCard key={project.id} project={project} />
          ))}
        </div>
      </div>
    </section>
  );
}
