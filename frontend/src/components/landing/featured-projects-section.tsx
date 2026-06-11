import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { ProjectCard } from "@/components/landing/project-card";
import { featuredProjects } from "@/lib/landing-data";

export function FeaturedProjectsSection() {
  return (
    <section className="bg-background py-16 sm:py-20">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col justify-between gap-4 md:flex-row md:items-end">
          <div>
            <h2 className="text-3xl font-semibold tracking-normal">Dự án nổi bật</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-muted-foreground">
              Khám phá những dự án đang tìm kiếm cộng sự và cơ hội hợp tác.
            </p>
          </div>
          <Link href="/projects" className="inline-flex h-10 items-center gap-2 rounded-md border border-border px-4 text-sm font-medium hover:bg-accent">
            Xem tất cả dự án
            <ArrowRight className="h-4 w-4" />
          </Link>
        </div>

        <div className="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {featuredProjects.map((project) => (
            <ProjectCard key={project.id} project={project} />
          ))}
        </div>
      </div>
    </section>
  );
}
