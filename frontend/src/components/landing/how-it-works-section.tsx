import { howItWorksSteps } from "@/lib/landing-data";

export function HowItWorksSection() {
  return (
    <section id="how-it-works" className="bg-background py-16 sm:py-20">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-3xl text-center">
          <h2 className="text-3xl font-semibold">Từ ý tưởng đến đội ngũ chỉ trong vài bước</h2>
          <p className="mt-3 text-sm leading-6 text-muted-foreground">
            StartupConnect giúp bạn trình bày dự án rõ ràng, nhận hỗ trợ từ AI và kết nối với đúng người.
          </p>
        </div>
        <div className="relative mt-10 grid gap-4 lg:grid-cols-3">
          <div className="absolute left-1/2 top-10 hidden h-px w-2/3 -translate-x-1/2 bg-border lg:block" />
          {howItWorksSteps.map((step, index) => {
            const Icon = step.icon;
            return (
              <article key={step.title} className="relative rounded-2xl border border-border bg-card p-6 text-center shadow-sm">
                <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-primary text-primary-foreground">
                  <Icon className="h-6 w-6" />
                </div>
                <p className="mt-5 text-sm font-semibold text-primary">Bước {index + 1}</p>
                <h3 className="mt-2 text-lg font-semibold">{step.title}</h3>
                <p className="mt-3 text-sm leading-6 text-muted-foreground">{step.description}</p>
              </article>
            );
          })}
        </div>
      </div>
    </section>
  );
}
