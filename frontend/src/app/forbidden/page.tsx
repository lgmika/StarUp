import Link from "next/link";
import { ShieldAlert } from "lucide-react";

export default function ForbiddenPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-4">
      <section className="w-full max-w-md rounded-lg border border-border bg-card p-6 text-center shadow-sm">
        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-md bg-destructive/10 text-destructive">
          <ShieldAlert className="h-6 w-6" />
        </div>
        <h1 className="mt-5 text-xl font-semibold">Access forbidden</h1>
        <p className="mt-2 text-sm leading-6 text-muted-foreground">
          Your account does not have permission to view this area. The backend remains the final authority for access.
        </p>
        <div className="mt-6 flex flex-col gap-2 sm:flex-row sm:justify-center">
          <Link
            className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
            href="/"
          >
            Back to console
          </Link>
          <Link
            className="inline-flex h-10 items-center justify-center rounded-md border border-border bg-background px-4 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground"
            href="/auth/login"
          >
            Sign in again
          </Link>
        </div>
      </section>
    </main>
  );
}
