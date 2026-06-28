import Link from "next/link";
import type { ReactNode } from "react";
import {
  Activity,
  Lightbulb,
  ShieldCheck,
  Sparkles,
  UsersRound,
} from "lucide-react";

interface AuthShellProps {
  title: string;
  description: string;
  children: ReactNode;
  footer: ReactNode;
}

export function AuthShell({
  title,
  description,
  children,
  footer,
}: AuthShellProps) {
  return (
    <main className="min-h-screen bg-background">
      <div className="mx-auto grid min-h-screen max-w-6xl gap-8 px-4 py-6 sm:px-6 lg:grid-cols-[1fr_440px] lg:items-center lg:px-8">
        {/* Left panel — branding & value props */}
        <section className="relative flex min-h-[280px] flex-col justify-between overflow-hidden rounded-2xl border border-border/60 bg-gradient-to-br from-primary/[0.03] via-card to-card p-6 shadow-lg lg:min-h-[640px] lg:p-8">
          {/* Decorative elements */}
          <div className="pointer-events-none absolute inset-0 overflow-hidden">
            <div className="absolute -right-20 -top-20 h-56 w-56 rounded-full bg-primary/5 blur-3xl" />
            <div className="absolute -bottom-20 -left-20 h-56 w-56 rounded-full bg-blue-500/5 blur-3xl" />
          </div>

          <div className="relative">
            <Link href="/" className="inline-flex items-center gap-3 group">
              <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-md shadow-primary/25 transition-transform group-hover:scale-105">
                <Lightbulb className="h-5 w-5" />
              </span>
              <span>
                <span className="block text-sm font-bold">
                  StartupConnect
                </span>
                <span className="block text-xs text-muted-foreground">
                  Founder & investor platform
                </span>
              </span>
            </Link>
          </div>

          <div className="relative max-w-xl py-10">
            <div className="inline-flex items-center gap-1.5 rounded-full bg-primary/10 px-3 py-1 text-xs font-semibold text-primary">
              <Sparkles className="h-3.5 w-3.5" />
              Authentication
            </div>
            <h1 className="mt-4 text-3xl font-bold tracking-tight text-foreground sm:text-4xl">
              Secure access for building{" "}
              <span className="bg-gradient-to-r from-primary to-blue-500 bg-clip-text text-transparent">
                startup teams
              </span>
            </h1>
            <p className="mt-4 text-sm leading-7 text-muted-foreground sm:text-base">
              Sign in to manage projects, review opportunities, and connect with
              the right people to build your startup.
            </p>
          </div>

          <div className="relative grid gap-3 sm:grid-cols-3">
            <Feature
              icon={<ShieldCheck className="h-4 w-4" />}
              title="Secure auth"
              text="JWT tokens with auto-refresh"
            />
            <Feature
              icon={<UsersRound className="h-4 w-4" />}
              title="Role-based"
              text="6 roles with granular access"
            />
            <Feature
              icon={<Activity className="h-4 w-4" />}
              title="Realtime"
              text="Live updates via SignalR"
            />
          </div>
        </section>

        {/* Right panel — form */}
        <section className="rounded-2xl border border-border/60 bg-card p-6 shadow-lg sm:p-8">
          <div className="mb-6">
            <h2 className="text-xl font-bold tracking-tight">{title}</h2>
            <p className="mt-2 text-sm leading-6 text-muted-foreground">
              {description}
            </p>
          </div>
          {children}
          <div className="mt-6 border-t border-border pt-5 text-center text-sm text-muted-foreground">
            {footer}
          </div>
        </section>
      </div>
    </main>
  );
}

function Feature({
  icon,
  title,
  text,
}: {
  icon: ReactNode;
  title: string;
  text: string;
}) {
  return (
    <div className="rounded-xl border border-border/40 bg-background/80 p-3 backdrop-blur-sm">
      <div className="flex items-center gap-2 text-sm font-semibold">
        <span className="text-primary">{icon}</span>
        {title}
      </div>
      <p className="mt-1.5 text-xs leading-5 text-muted-foreground">{text}</p>
    </div>
  );
}
