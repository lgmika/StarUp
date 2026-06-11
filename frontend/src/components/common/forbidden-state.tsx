import Link from "next/link";
import { ShieldAlert } from "lucide-react";

export function ForbiddenState() {
  return (
    <section className="mx-auto flex min-h-[420px] max-w-md flex-col items-center justify-center text-center">
      <div className="flex h-12 w-12 items-center justify-center rounded-md bg-destructive/10 text-destructive">
        <ShieldAlert className="h-6 w-6" />
      </div>
      <h1 className="mt-5 text-xl font-semibold">Access forbidden</h1>
      <p className="mt-2 text-sm leading-6 text-muted-foreground">
        Your account does not have permission to view this area. Backend authorization remains the final source of truth.
      </p>
      <div className="mt-6 flex flex-col gap-2 sm:flex-row">
        <Link
          className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
          href="/dashboard"
        >
          Back to dashboard
        </Link>
        <Link
          className="inline-flex h-10 items-center justify-center rounded-md border border-border bg-background px-4 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground"
          href="/auth/login"
        >
          Sign in again
        </Link>
      </div>
    </section>
  );
}
