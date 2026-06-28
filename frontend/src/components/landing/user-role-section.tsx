import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { roleCards } from "@/lib/landing-data";

const cardGradients = [
  "from-blue-500/10 to-blue-500/5",
  "from-emerald-500/10 to-emerald-500/5",
  "from-purple-500/10 to-purple-500/5",
];

const iconColors = [
  "bg-blue-100 text-blue-700",
  "bg-emerald-100 text-emerald-700",
  "bg-purple-100 text-purple-700",
];

export function UserRoleSection() {
  return (
    <section className="border-y border-border bg-muted/30 py-16 sm:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mx-auto mb-12 max-w-3xl text-center">
          <span className="inline-flex items-center rounded-full bg-primary/10 px-3 py-1 text-xs font-semibold text-primary">
            Dành cho mọi người
          </span>
          <h2 className="mt-4 text-3xl font-bold tracking-tight sm:text-4xl">
            Bạn là ai trong hành trình khởi nghiệp?
          </h2>
        </div>
        <div className="grid gap-6 lg:grid-cols-3">
          {roleCards.map((role, index) => (
            <article
              key={role.title}
              id={role.id}
              className={`group relative overflow-hidden rounded-2xl border border-border/60 bg-card p-8 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-primary/20 hover:shadow-lg`}
            >
              {/* Gradient background */}
              <div
                className={`absolute inset-0 bg-gradient-to-br ${cardGradients[index]} opacity-0 transition-opacity duration-300 group-hover:opacity-100`}
              />
              <div className="relative">
                <div
                  className={`inline-flex h-10 items-center rounded-lg px-3 text-sm font-semibold ${iconColors[index]}`}
                >
                  {role.title}
                </div>
                <h2 className="mt-4 text-2xl font-bold">{role.question}</h2>
                <p className="mt-3 min-h-16 text-sm leading-6 text-muted-foreground">
                  {role.description}
                </p>
                <Link
                  href={role.href}
                  className="mt-6 inline-flex h-11 items-center gap-2 rounded-xl bg-primary px-5 text-sm font-semibold text-primary-foreground shadow-md shadow-primary/20 transition-all hover:bg-primary/90 hover:shadow-lg hover:shadow-primary/25"
                >
                  {role.cta}
                  <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
                </Link>
              </div>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}
