"use client";

import type { ReactNode } from "react";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { useCurrentUser } from "@/hooks/use-current-user";

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { user, isLoading, sessionError, retrySession } = useCurrentUser({ requireAuth: true });

  if (!isLoading && !user && sessionError) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-background p-4">
        <div className="max-w-md text-center">
          <h1 className="text-xl font-semibold">Session check unavailable</h1>
          <p className="mt-2 text-sm text-muted-foreground">{sessionError}</p>
          <Button className="mt-5" onClick={() => void retrySession().catch(() => undefined)}>Try again</Button>
        </div>
      </main>
    );
  }

  if (isLoading || !user) {
    return (
      <main className="min-h-screen bg-background p-4">
        <LoadingState label="Checking your session" />
      </main>
    );
  }

  return children;
}
