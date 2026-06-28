"use client";

import { Target, Lightbulb, UsersRound, Building2 } from "lucide-react";

import { Badge } from "@/components/ui/badge";

const STATS = [
  { label: "Active Startups", value: "4,000+" },
  { label: "Investors", value: "850+" },
  { label: "Capital Raised", value: "$120M+" },
  { label: "Talent Members", value: "25,000+" },
];

const VALUES = [
  {
    title: "Founder First",
    description: "We build tools that remove friction for founders, so they can focus on building their product.",
    icon: Target,
  },
  {
    title: "Data-Driven Matching",
    description: "Our AI systems ensure that the right investors meet the right startups at the right time.",
    icon: Lightbulb,
  },
  {
    title: "Community Trust",
    description: "We maintain a high-quality ecosystem through strict moderation and verified profiles.",
    icon: UsersRound,
  },
];

export default function AboutPage() {
  return (
    <main className="bg-background">
      
      {/* Hero Section */}
      <div className="relative overflow-hidden py-24 sm:py-32">
        <div className="mx-auto max-w-7xl px-6 lg:px-8 text-center">
          <Badge tone="default" className="bg-primary/10 text-primary mb-6">Our Mission</Badge>
          <h1 className="text-4xl font-bold tracking-tight text-foreground sm:text-6xl max-w-4xl mx-auto">
            Democratizing access to startup capital and talent.
          </h1>
          <p className="mx-auto mt-6 max-w-2xl text-lg leading-8 text-muted-foreground">
            StartupConnect was founded in 2024 with a simple belief: great ideas shouldn&apos;t fail just because founders don&apos;t have the right network. We are building the infrastructure for the next generation of innovation.
          </p>
        </div>
      </div>

      {/* Stats Section */}
      <div className="bg-muted/30 py-16 sm:py-24 border-y border-border/50">
        <div className="mx-auto max-w-7xl px-6 lg:px-8">
          <dl className="grid grid-cols-1 gap-x-8 gap-y-16 text-center sm:grid-cols-2 lg:grid-cols-4">
            {STATS.map((stat) => (
              <div key={stat.label} className="mx-auto flex max-w-xs flex-col gap-y-4">
                <dt className="text-base leading-7 text-muted-foreground">{stat.label}</dt>
                <dd className="order-first text-3xl font-bold tracking-tight text-foreground sm:text-5xl">
                  {stat.value}
                </dd>
              </div>
            ))}
          </dl>
        </div>
      </div>

      {/* Values Section */}
      <div className="mx-auto max-w-7xl px-6 py-24 sm:py-32 lg:px-8">
        <div className="mx-auto max-w-2xl lg:text-center">
          <h2 className="text-base font-semibold leading-7 text-primary">Our Values</h2>
          <p className="mt-2 text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
            Everything you need to scale
          </p>
          <p className="mt-6 text-lg leading-8 text-muted-foreground">
            We operate on a core set of principles designed to foster innovation, transparency, and success.
          </p>
        </div>
        <div className="mx-auto mt-16 max-w-2xl sm:mt-20 lg:mt-24 lg:max-w-none">
          <dl className="grid max-w-xl grid-cols-1 gap-x-8 gap-y-16 lg:max-w-none lg:grid-cols-3">
            {VALUES.map((value) => (
              <div key={value.title} className="flex flex-col rounded-3xl border border-border/50 bg-card p-8 shadow-sm">
                <dt className="flex items-center gap-x-3 text-lg font-semibold leading-7 text-foreground">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
                    <value.icon className="h-5 w-5" aria-hidden="true" />
                  </div>
                  {value.title}
                </dt>
                <dd className="mt-4 flex flex-auto flex-col text-base leading-7 text-muted-foreground">
                  <p className="flex-auto">{value.description}</p>
                </dd>
              </div>
            ))}
          </dl>
        </div>
      </div>

      {/* Offices / Contact */}
      <div className="mx-auto max-w-7xl px-6 pb-24 lg:px-8">
        <div className="rounded-3xl bg-primary/5 p-8 sm:p-16 border border-primary/10 text-center">
          <Building2 className="mx-auto h-12 w-12 text-primary/60 mb-6" />
          <h2 className="text-2xl font-bold tracking-tight text-foreground">Global Headquarters</h2>
          <p className="mt-4 text-muted-foreground">
            123 Innovation Drive<br />
            San Francisco, CA 94105<br />
            contact@startupconnect.dev
          </p>
        </div>
      </div>

    </main>
  );
}
