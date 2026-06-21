"use client";

import { useEffect, useRef, useState } from "react";
import { QueryClientProvider, useQueryClient } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { createQueryClient } from "@/lib/query-client";
import { RealtimeProvider } from "./realtime-provider";
import { useAuthStore } from "@/stores/auth-store";

export function AppProviders({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(createQueryClient);

  return (
    <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
      <QueryClientProvider client={queryClient}>
        <AuthCacheSync />
        <RealtimeProvider>{children}</RealtimeProvider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}

function AuthCacheSync() {
  const queryClient = useQueryClient();
  const userId = useAuthStore((state) => state.user?.id ?? null);
  const previousUserId = useRef<string | null | undefined>(undefined);

  useEffect(() => {
    const clearSession = () => useAuthStore.getState().logout();
    window.addEventListener("startupconnect:session-cleared", clearSession);
    return () => window.removeEventListener("startupconnect:session-cleared", clearSession);
  }, []);

  useEffect(() => {
    if (previousUserId.current !== undefined && previousUserId.current !== userId) {
      queryClient.clear();
    }
    previousUserId.current = userId;
  }, [queryClient, userId]);

  return null;
}
