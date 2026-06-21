"use client";

import { useEffect, useState } from "react";
import { useRouter, usePathname } from "next/navigation";
import { getAccessToken } from "@/lib/auth";
import { useAuthStore } from "@/stores/auth-store";

export function useCurrentUser({ requireAuth = false }: { requireAuth?: boolean } = {}) {
  const router = useRouter();
  const pathname = usePathname();
  const user = useAuthStore((state) => state.user);
  const isLoading = useAuthStore((state) => state.isLoading);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const sessionError = useAuthStore((state) => state.sessionError);
  const loadCurrentUser = useAuthStore((state) => state.loadCurrentUser);
  const setLoading = useAuthStore((state) => state.setLoading);
  const [hasCheckedSession, setHasCheckedSession] = useState(false);

  useEffect(() => {
    let isMounted = true;

    async function hydrateSession() {
      const token = getAccessToken();

      if (!token) {
        setLoading(false);
        setHasCheckedSession(true);
        if (requireAuth) {
          router.replace(`/auth/login?next=${encodeURIComponent(pathname)}`);
        }
        return;
      }

      if (user) {
        setLoading(false);
        setHasCheckedSession(true);
        return;
      }

      try {
        await loadCurrentUser({ allowAnonymous: !requireAuth });
        if (isMounted) setHasCheckedSession(true);
      } catch {
        if (isMounted && requireAuth && !getAccessToken()) {
          router.replace(`/auth/login?next=${encodeURIComponent(pathname)}`);
        }
        if (isMounted) setHasCheckedSession(true);
      }
    }

    void hydrateSession();

    return () => {
      isMounted = false;
    };
  }, [loadCurrentUser, pathname, requireAuth, router, setLoading, user]);

  return {
    user,
    isLoading: isLoading || !hasCheckedSession,
    isAuthenticated,
    sessionError,
    retrySession: () => loadCurrentUser({ allowAnonymous: !requireAuth }),
  };
}
