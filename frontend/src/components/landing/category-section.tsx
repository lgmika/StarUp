import Link from "next/link";
import { categories } from "@/lib/landing-data";

export function CategorySection() {
  return (
    <section className="border-y border-border bg-muted/40 py-16 sm:py-20">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="max-w-2xl">
          <h2 className="text-3xl font-semibold">Khám phá theo lĩnh vực</h2>
          <p className="mt-3 text-sm leading-6 text-muted-foreground">
            Đi thẳng vào nhóm dự án phù hợp với chuyên môn, sở thích hoặc chiến lược đầu tư của bạn.
          </p>
        </div>
        <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {categories.map((category) => {
            const Icon = category.icon;
            return (
              <Link
                key={category.field}
                href={`/projects?field=${encodeURIComponent(category.field)}`}
                className="rounded-2xl border border-border bg-card p-5 shadow-sm transition-transform hover:-translate-y-1 hover:shadow-md"
              >
                <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary/10 text-primary">
                  <Icon className="h-5 w-5" />
                </div>
                <h3 className="mt-4 text-base font-semibold">{category.name}</h3>
                <p className="mt-2 text-sm leading-6 text-muted-foreground">{category.description}</p>
              </Link>
            );
          })}
        </div>
      </div>
    </section>
  );
}
