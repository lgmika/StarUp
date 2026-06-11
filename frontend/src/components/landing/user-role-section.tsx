import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { roleCards } from "@/lib/landing-data";

export function UserRoleSection() {
  return (
    <section className="border-y border-border bg-muted/40 py-16 sm:py-20">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="grid gap-4 lg:grid-cols-3">
          {roleCards.map((role) => (
            <article key={role.title} id={role.id} className="rounded-2xl border border-border bg-card p-6 shadow-sm">
              <p className="text-sm font-semibold text-primary">{role.title}</p>
              <h2 className="mt-3 text-2xl font-semibold">{role.question}</h2>
              <p className="mt-3 min-h-16 text-sm leading-6 text-muted-foreground">{role.description}</p>
              <Link
                href={role.href}
                className="mt-6 inline-flex h-10 items-center gap-2 rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/90"
              >
                {role.cta}
                <ArrowRight className="h-4 w-4" />
              </Link>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}
