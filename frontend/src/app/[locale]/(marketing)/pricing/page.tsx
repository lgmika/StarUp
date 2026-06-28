"use client";

import { Check, X } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";

const PLANS = [
  {
    name: "Member",
    price: "Free",
    description: "For individuals looking to join projects and find co-founders.",
    features: [
      "Create 1 basic profile",
      "Upload up to 2 CVs",
      "Apply to 5 projects per month",
      "Basic search filters",
      "Standard support",
    ],
    missing: ["Create projects", "Contact investors", "AI project reviews", "Custom NDA templates"],
    cta: "Get Started",
    popular: false,
  },
  {
    name: "Founder Pro",
    price: "$19",
    period: "/month",
    description: "Everything a founder needs to build a team and raise capital.",
    features: [
      "Create up to 3 projects",
      "Unlimited member applications",
      "Contact up to 10 investors/month",
      "Advanced search & recommendations",
      "3 AI project reviews/month",
      "Basic NDA templates",
      "Priority support",
    ],
    missing: ["Custom NDA templates", "Dedicated account manager"],
    cta: "Start 14-day free trial",
    popular: true,
  },
  {
    name: "Investor & Enterprise",
    price: "$99",
    period: "/month",
    description: "For angel investors, VC firms, and accelerators finding deal flow.",
    features: [
      "Unlimited project viewing",
      "Unlimited founder contact",
      "Advanced deal flow analytics",
      "Export reports to PDF/CSV",
      "Unlimited AI risk assessments",
      "Custom NDA templates & e-signatures",
      "Dedicated account manager",
    ],
    missing: [],
    cta: "Contact Sales",
    popular: false,
  },
];

export default function PricingPage() {
  return (
    <main className="bg-background">
      <div className="relative overflow-hidden bg-background py-24 sm:py-32">
        <div className="absolute inset-0 -z-10 bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-primary/10 via-background to-background" />
        <div className="mx-auto max-w-7xl px-6 lg:px-8">
          <div className="mx-auto max-w-4xl text-center">
            <h1 className="text-base font-semibold leading-7 text-primary">Pricing</h1>
            <p className="mt-2 text-4xl font-bold tracking-tight text-foreground sm:text-5xl">
              Pricing plans for teams of all sizes
            </p>
            <p className="mt-6 text-lg leading-8 text-muted-foreground">
              Choose the perfect plan for your needs. Whether you&apos;re a solo developer, an ambitious founder, or a venture capitalist.
            </p>
          </div>

          <div className="isolate mx-auto mt-16 grid max-w-md grid-cols-1 gap-8 lg:mx-0 lg:max-w-none lg:grid-cols-3">
            {PLANS.map((plan) => (
              <div
                key={plan.name}
                className={`flex flex-col justify-between rounded-3xl p-8 ring-1 sm:p-10 ${
                  plan.popular
                    ? "bg-primary/5 ring-primary/50"
                    : "bg-card ring-border/50"
                }`}
              >
                <div>
                  <div className="flex items-center justify-between gap-x-4">
                    <h3 className="text-lg font-semibold leading-8 text-foreground">
                      {plan.name}
                    </h3>
                    {plan.popular ? (
                      <Badge tone="default" className="bg-primary/10 text-primary">
                        Most popular
                      </Badge>
                    ) : null}
                  </div>
                  <p className="mt-4 text-sm leading-6 text-muted-foreground">
                    {plan.description}
                  </p>
                  <p className="mt-6 flex items-baseline gap-x-1">
                    <span className="text-4xl font-bold tracking-tight text-foreground">
                      {plan.price}
                    </span>
                    {plan.period ? (
                      <span className="text-sm font-semibold leading-6 text-muted-foreground">
                        {plan.period}
                      </span>
                    ) : null}
                  </p>
                  <ul role="list" className="mt-8 space-y-3 text-sm leading-6 text-muted-foreground">
                    {plan.features.map((feature) => (
                      <li key={feature} className="flex gap-x-3">
                        <Check className="h-6 w-5 flex-none text-primary" aria-hidden="true" />
                        {feature}
                      </li>
                    ))}
                    {plan.missing.map((feature) => (
                      <li key={feature} className="flex gap-x-3 text-muted-foreground/50">
                        <X className="h-6 w-5 flex-none" aria-hidden="true" />
                        {feature}
                      </li>
                    ))}
                  </ul>
                </div>
                <Button
                  className="mt-8 block w-full rounded-xl"
                  variant={plan.popular ? "primary" : "outline"}
                >
                  {plan.cta}
                </Button>
              </div>
            ))}
          </div>
        </div>
      </div>
    </main>
  );
}
