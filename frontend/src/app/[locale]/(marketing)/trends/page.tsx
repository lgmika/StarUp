"use client";

import { TrendingUp, BarChart3, Activity, ArrowUpRight } from "lucide-react";


const TRENDS = [
  {
    category: "Artificial Intelligence",
    growth: "+145%",
    description: "Generative AI applications in enterprise workflows continue to dominate seed funding rounds.",
    projects: 1240,
    color: "bg-blue-500",
  },
  {
    category: "Climate Tech",
    growth: "+85%",
    description: "Carbon accounting and renewable energy grid solutions are seeing massive Series A interest.",
    projects: 850,
    color: "bg-emerald-500",
  },
  {
    category: "FinTech & Web3",
    growth: "+42%",
    description: "Cross-border payment infrastructure and RWA (Real World Assets) tokenization are the top trends.",
    projects: 620,
    color: "bg-purple-500",
  },
  {
    category: "HealthTech",
    growth: "+67%",
    description: "Telehealth platforms and AI-driven diagnostic tools are experiencing a resurgence post-2024.",
    projects: 930,
    color: "bg-rose-500",
  },
];

import { Badge } from "@/components/ui/badge";

export default function TrendsPage() {
  return (
    <main className="bg-background">
      
      {/* Hero Section */}
      <div className="relative overflow-hidden bg-background py-24 sm:py-32">
        <div className="absolute inset-x-0 top-0 -z-10 h-full w-full bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-primary/10 via-background to-background" />
        <div className="mx-auto max-w-7xl px-6 lg:px-8">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-8">
            <div className="max-w-2xl">
              <Badge tone="default" className="bg-primary/10 text-primary mb-6">Q2 2026 Report</Badge>
              <h1 className="text-4xl font-bold tracking-tight text-foreground sm:text-5xl">
                Startup Market Trends
              </h1>
              <p className="mt-4 text-lg text-muted-foreground">
                Real-time data and insights aggregated from thousands of projects and investor interactions on our platform.
              </p>
            </div>
            <div className="flex gap-4 shrink-0">
              <div className="rounded-2xl border border-border/50 bg-card p-6 shadow-sm">
                <div className="flex items-center gap-2 text-muted-foreground mb-2">
                  <Activity className="h-4 w-4" />
                  <span className="text-sm font-medium">Platform Activity</span>
                </div>
                <div className="text-3xl font-bold text-foreground">High</div>
                <div className="mt-1 flex items-center text-xs text-emerald-500 font-medium">
                  <ArrowUpRight className="h-3 w-3 mr-1" />
                  +12% vs last month
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Trends Grid */}
      <div className="mx-auto max-w-7xl px-6 py-16 lg:px-8">
        <div className="flex items-center gap-2 mb-8">
          <BarChart3 className="h-6 w-6 text-primary" />
          <h2 className="text-2xl font-bold tracking-tight">Hottest Sectors</h2>
        </div>
        
        <div className="grid gap-6 md:grid-cols-2">
          {TRENDS.map((trend) => (
            <div key={trend.category} className="group rounded-3xl border border-border/50 bg-card p-8 shadow-sm transition-all hover:-translate-y-1 hover:shadow-md">
              <div className="flex items-start justify-between mb-6">
                <div>
                  <h3 className="text-xl font-bold text-foreground group-hover:text-primary transition-colors">
                    {trend.category}
                  </h3>
                  <p className="text-sm font-medium text-muted-foreground mt-1">
                    {trend.projects.toLocaleString()} active projects
                  </p>
                </div>
                <div className="flex flex-col items-end">
                  <Badge tone="success" className="bg-emerald-500/10 text-emerald-600 font-bold px-3 py-1 text-sm">
                    {trend.growth} YoY
                  </Badge>
                  <TrendingUp className="h-4 w-4 text-emerald-500 mt-2 opacity-50" />
                </div>
              </div>
              
              <p className="text-sm leading-relaxed text-muted-foreground">
                {trend.description}
              </p>
              
              <div className="mt-8 h-2 w-full rounded-full bg-muted overflow-hidden">
                <div className={`h-full ${trend.color} opacity-80`} style={{ width: '70%' }} />
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Newsletter CTA */}
      <div className="mx-auto max-w-7xl px-6 pb-24 lg:px-8">
        <div className="rounded-3xl bg-muted/50 p-8 sm:p-16 border border-border/50 text-center">
          <h2 className="text-2xl font-bold tracking-tight text-foreground">Get weekly market insights</h2>
          <p className="mt-4 text-muted-foreground max-w-xl mx-auto mb-8">
            Subscribe to our newsletter to receive the latest funding trends, pitch deck teardowns, and platform updates directly in your inbox.
          </p>
          <div className="mx-auto flex max-w-md gap-x-4">
            <input
              id="email-address"
              name="email"
              type="email"
              autoComplete="email"
              required
              className="min-w-0 flex-auto rounded-xl border-0 bg-background px-3.5 py-2 text-foreground shadow-sm ring-1 ring-inset ring-border focus:ring-2 focus:ring-inset focus:ring-primary sm:text-sm sm:leading-6 outline-none"
              placeholder="Enter your email"
            />
            <button
              type="submit"
              className="flex-none rounded-xl bg-primary px-3.5 py-2.5 text-sm font-semibold text-primary-foreground shadow-sm hover:bg-primary/90 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
            >
              Subscribe
            </button>
          </div>
        </div>
      </div>

    </main>
  );
}
