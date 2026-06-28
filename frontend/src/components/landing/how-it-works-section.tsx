"use client";

import { howItWorksSteps } from "@/lib/landing-data";

export function HowItWorksSection() {
  return (
    <section id="how-it-works" className="bg-background py-16 sm:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-3xl text-center">
          <span className="inline-flex items-center rounded-full bg-primary/10 px-3 py-1 text-xs font-semibold text-primary">
            Cách hoạt động
          </span>
          <h2 className="mt-4 text-3xl font-bold tracking-tight sm:text-4xl">
            Từ ý tưởng đến đội ngũ chỉ trong vài bước
          </h2>
          <p className="mt-4 text-base leading-7 text-muted-foreground">
            StartupConnect giúp bạn trình bày dự án rõ ràng, nhận hỗ trợ từ AI
            và kết nối với đúng người.
          </p>
        </div>

        <div className="relative mt-16 grid gap-6 lg:grid-cols-3">
          {/* Connecting line */}
          <div className="absolute left-[16.67%] right-[16.67%] top-16 hidden h-0.5 bg-gradient-to-r from-transparent via-border to-transparent lg:block" />

          {howItWorksSteps.map((step, index) => {
            const Icon = step.icon;
            return (
              <article
                key={step.title}
                className="group relative rounded-2xl border border-border/60 bg-card p-8 text-center shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-primary/20 hover:shadow-lg"
              >
                {/* Step number */}
                <div className="absolute -top-4 left-1/2 flex h-8 w-8 -translate-x-1/2 items-center justify-center rounded-full bg-primary text-sm font-bold text-primary-foreground shadow-md shadow-primary/25">
                  {index + 1}
                </div>

                <div className="mx-auto mt-2 flex h-16 w-16 items-center justify-center rounded-2xl bg-gradient-to-br from-primary/10 to-primary/5 text-primary transition-transform duration-300 group-hover:scale-110">
                  <Icon className="h-7 w-7" />
                </div>
                <h3 className="mt-5 text-lg font-bold">{step.title}</h3>
                <p className="mt-3 text-sm leading-6 text-muted-foreground">
                  {step.description}
                </p>
              </article>
            );
          })}
        </div>
      </div>
    </section>
  );
}
