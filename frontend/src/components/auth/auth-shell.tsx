import Link from "next/link";
import type { ReactNode } from "react";
import { Activity, ShieldCheck, UsersRound } from "lucide-react";

interface AuthShellProps {
  title: string;
  description: string;
  children: ReactNode;
  footer: ReactNode;
}

export function AuthShell({ title, description, children, footer }: AuthShellProps) {
  return (
    <main className="min-h-screen bg-background">
      <div className="mx-auto grid min-h-screen max-w-6xl gap-8 px-4 py-6 sm:px-6 lg:grid-cols-[1fr_440px] lg:items-center lg:px-8">
        <section className="flex min-h-[280px] flex-col justify-between rounded-lg border border-border bg-card p-6 shadow-sm lg:min-h-[620px] lg:p-8">
          <div>
            <Link href="/" className="inline-flex items-center gap-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-md bg-primary text-primary-foreground">
                <Activity className="h-5 w-5" />
              </span>
              <span>
                <span className="block text-sm font-semibold">StartupConnect</span>
                <span className="block text-xs text-muted-foreground">Founder and investor console</span>
              </span>
            </Link>
          </div>

          <div className="max-w-xl py-10">
            <p className="text-sm font-medium text-primary">Authentication</p>
            <h1 className="mt-3 text-3xl font-semibold tracking-normal text-foreground sm:text-4xl">
              Secure access for building startup teams.
            </h1>
            <p className="mt-4 text-sm leading-6 text-muted-foreground sm:text-base">
              Sign in to manage projects, review opportunities, and keep founder collaboration workflows connected to the API.
            </p>
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            <Feature icon={<ShieldCheck className="h-4 w-4" />} title="Token based" text="Access and refresh tokens are stored through the shared auth helper." />
            <Feature icon={<UsersRound className="h-4 w-4" />} title="Role aware" text="Current user data is loaded from the backend auth profile endpoint." />
          </div>
        </section>

        <section className="rounded-lg border border-border bg-card p-5 shadow-sm sm:p-6">
          <div className="mb-6">
            <h2 className="text-xl font-semibold">{title}</h2>
            <p className="mt-2 text-sm leading-6 text-muted-foreground">{description}</p>
          </div>
          {children}
          <div className="mt-6 border-t border-border pt-5 text-center text-sm text-muted-foreground">{footer}</div>
        </section>
      </div>
    </main>
  );
}

function Feature({ icon, title, text }: { icon: ReactNode; title: string; text: string }) {
  return (
    <div className="rounded-md border border-border bg-background p-3">
      <div className="flex items-center gap-2 text-sm font-medium">
        <span className="text-primary">{icon}</span>
        {title}
      </div>
      <p className="mt-2 text-xs leading-5 text-muted-foreground">{text}</p>
    </div>
  );
}
