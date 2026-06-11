"use client";

import type { ReactNode } from "react";
import { LoadingState } from "@/components/common/loading-state";
import { useCurrentUser } from "@/hooks/use-current-user";

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { user, isLoading } = useCurrentUser({ requireAuth: true });

  if (isLoading || !user) {
    return (
      <main className="min-h-screen bg-background p-4">
        <LoadingState label="Checking your session" />
      </main>
    );
  }

  return children;
}
