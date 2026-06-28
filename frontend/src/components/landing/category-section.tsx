import Link from "next/link";
import { categories } from "@/lib/landing-data";

const iconBgColors = [
  "bg-violet-100 text-violet-700",
  "bg-blue-100 text-blue-700",
  "bg-emerald-100 text-emerald-700",
  "bg-rose-100 text-rose-700",
  "bg-amber-100 text-amber-700",
  "bg-teal-100 text-teal-700",
  "bg-lime-100 text-lime-700",
  "bg-orange-100 text-orange-700",
];

export function CategorySection() {
  return (
    <section className="border-y border-border bg-muted/30 py-16 sm:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="max-w-2xl">
          <span className="inline-flex items-center rounded-full bg-primary/10 px-3 py-1 text-xs font-semibold text-primary">
            Lĩnh vực
          </span>
          <h2 className="mt-4 text-3xl font-bold tracking-tight sm:text-4xl">
            Khám phá theo lĩnh vực
          </h2>
          <p className="mt-4 text-base leading-7 text-muted-foreground">
            Đi thẳng vào nhóm dự án phù hợp với chuyên môn, sở thích hoặc chiến
            lược đầu tư của bạn.
          </p>
        </div>
        <div className="mt-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {categories.map((category, index) => {
            const Icon = category.icon;
            return (
              <Link
                key={category.field}
                href={`/projects?field=${encodeURIComponent(category.field)}`}
                className="group rounded-2xl border border-border/60 bg-card p-6 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-primary/20 hover:shadow-lg"
              >
                <div
                  className={`flex h-12 w-12 items-center justify-center rounded-xl ${iconBgColors[index % iconBgColors.length]} transition-transform duration-300 group-hover:scale-110`}
                >
                  <Icon className="h-5 w-5" />
                </div>
                <h3 className="mt-4 text-base font-bold">{category.name}</h3>
                <p className="mt-2 text-sm leading-6 text-muted-foreground">
                  {category.description}
                </p>
              </Link>
            );
          })}
        </div>
      </div>
    </section>
  );
}
