"use client";

import { Calendar, MapPin, Users, ArrowRight, Video } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";

const EVENTS = [
  {
    title: "Global Startup Pitch Day 2026",
    date: "July 15, 2026",
    type: "Online",
    attendees: "1,200+",
    category: "Pitching",
    description: "Join 50 top startups as they pitch their products to a panel of tier-1 venture capitalists. Great networking opportunity for early-stage founders.",
    image: "bg-blue-500",
  },
  {
    title: "AI in EdTech Workshop",
    date: "July 22, 2026",
    type: "San Francisco, CA",
    attendees: "300",
    category: "Workshop",
    description: "A hands-on workshop focused on integrating large language models into educational platforms. Sponsored by leading AI research labs.",
    image: "bg-emerald-500",
  },
  {
    title: "Founder & Investor Mixer",
    date: "August 5, 2026",
    type: "New York City, NY",
    attendees: "150",
    category: "Networking",
    description: "An exclusive, invite-only mixer for seed-stage founders and angel investors. Food, drinks, and high-value conversations.",
    image: "bg-purple-500",
  },
  {
    title: "Web3 Security Summit",
    date: "August 18, 2026",
    type: "Online",
    attendees: "800+",
    category: "Conference",
    description: "Learn about the latest vulnerabilities, auditing techniques, and security practices for decentralized applications.",
    image: "bg-amber-500",
  },
];

export default function EventsPage() {
  return (
    <main className="bg-background">
      
      {/* Hero Section */}
      <div className="relative overflow-hidden bg-background py-24 sm:py-32">
        <div className="absolute inset-x-0 top-0 -z-10 h-full w-full bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-primary/10 via-background to-background" />
        <div className="mx-auto max-w-7xl px-6 lg:px-8 text-center">
          <Badge tone="default" className="bg-primary/10 text-primary mb-6">Upcoming Events</Badge>
          <h1 className="text-4xl font-bold tracking-tight text-foreground sm:text-6xl">
            Connect, Learn, and Pitch
          </h1>
          <p className="mx-auto mt-6 max-w-2xl text-lg leading-8 text-muted-foreground">
            Discover workshops, networking mixers, and pitch days designed to help you build your team and secure funding.
          </p>
        </div>
      </div>

      {/* Events Grid */}
      <div className="mx-auto max-w-7xl px-6 py-16 lg:px-8">
        <div className="grid gap-8 md:grid-cols-2">
          {EVENTS.map((event, index) => (
            <div key={index} className="group relative overflow-hidden rounded-3xl border border-border/50 bg-card shadow-sm transition-all hover:shadow-md">
              <div className={`h-32 w-full opacity-20 ${event.image}`} />
              <div className="p-8">
                <div className="flex items-center justify-between mb-4">
                  <Badge tone="muted" className="text-muted-foreground">
                    {event.category}
                  </Badge>
                  <div className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                    <Calendar className="h-4 w-4" />
                    {event.date}
                  </div>
                </div>
                <h3 className="text-2xl font-bold text-foreground group-hover:text-primary transition-colors">
                  {event.title}
                </h3>
                <p className="mt-4 text-sm leading-relaxed text-muted-foreground">
                  {event.description}
                </p>
                <div className="mt-6 flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
                  <div className="flex items-center gap-1.5">
                    {event.type === "Online" ? <Video className="h-4 w-4" /> : <MapPin className="h-4 w-4" />}
                    {event.type}
                  </div>
                  <div className="flex items-center gap-1.5">
                    <Users className="h-4 w-4" />
                    {event.attendees} attendees
                  </div>
                </div>
                <div className="mt-8">
                  <Button variant="primary" className="w-full rounded-xl group-hover:bg-primary/90">
                    Register Now
                    <ArrowRight className="ml-2 h-4 w-4" />
                  </Button>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Host an event CTA */}
      <div className="mx-auto max-w-7xl px-6 py-16 lg:px-8 mb-12">
        <div className="flex flex-col md:flex-row items-center justify-between gap-8 rounded-3xl bg-muted/50 p-8 md:p-12 border border-border/50">
          <div>
            <h2 className="text-2xl font-bold tracking-tight text-foreground">Want to host an event?</h2>
            <p className="mt-2 text-muted-foreground">Partner with StartupConnect to reach thousands of founders and investors.</p>
          </div>
          <Button variant="outline" size="md" className="shrink-0 rounded-full bg-background">
            Contact Partnerships
          </Button>
        </div>
      </div>

    </main>
  );
}
