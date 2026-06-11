import { trustFeatures } from "@/lib/landing-data";

export function TrustSafetySection() {
  return (
    <section className="border-y border-border bg-muted/40 py-16 sm:py-20">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="max-w-3xl">
          <h2 className="text-3xl font-semibold">Chia sẻ ý tưởng với quyền kiểm soát của bạn</h2>
          <p className="mt-3 text-sm leading-6 text-muted-foreground">
            StartupConnect hỗ trợ visibility, NDA, version history và moderation để Founder chia sẻ đúng mức cần thiết.
          </p>
        </div>
        <div className="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {trustFeatures.map((feature) => {
            const Icon = feature.icon;
            return (
              <article key={feature.title} className="rounded-2xl border border-border bg-card p-5 shadow-sm">
                <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary/10 text-primary">
                  <Icon className="h-5 w-5" />
                </div>
                <h3 className="mt-4 text-base font-semibold">{feature.title}</h3>
                <p className="mt-2 text-sm leading-6 text-muted-foreground">{feature.description}</p>
              </article>
            );
          })}
        </div>
      </div>
    </section>
  );
}
