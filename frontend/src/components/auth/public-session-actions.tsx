"use client";

import Link from "next/link";
import { LogOut, RefreshCw, UserRound } from "lucide-react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { useCurrentUser } from "@/hooks/use-current-user";
import { getRoleHome } from "@/lib/permissions";
import { useAuthStore } from "@/stores/auth-store";

export function PublicSessionActions({ compact = false }: { compact?: boolean }) {
  const router = useRouter();
  const { user, isLoading, sessionError, retrySession } = useCurrentUser();
  const logoutRemote = useAuthStore((state) => state.logoutRemote);

  if (isLoading) return <div className="h-9 w-28 animate-pulse rounded-md bg-muted" />;
  if (!user && sessionError) return <Button size="sm" variant="outline" onClick={() => void retrySession().catch(() => undefined)}><RefreshCw className="h-4 w-4" />Retry session</Button>;
  if (!user) return <div className="flex items-center gap-2"><Link className="text-sm font-medium text-muted-foreground hover:text-foreground" href="/auth/login">Sign in</Link>{!compact ? <Link className="inline-flex h-9 items-center rounded-md bg-primary px-3 text-sm font-medium text-primary-foreground" href="/auth/register">Join</Link> : null}</div>;

  async function logout() {
    await logoutRemote();
    router.replace("/");
    router.refresh();
  }

  return <div className="flex items-center gap-2"><Link className="inline-flex h-9 items-center gap-2 rounded-md border border-border px-3 text-sm font-medium hover:bg-accent" href={getRoleHome(user.roles)}><UserRound className="h-4 w-4" />{compact ? "Workspace" : user.fullName}</Link><Button size="icon" variant="ghost" aria-label="Logout" onClick={logout}><LogOut className="h-4 w-4" /></Button></div>;
}
