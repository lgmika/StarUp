"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import type { ReactNode } from "react";
import { Activity, LogOut, Menu } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { getPrimaryRole } from "@/lib/permissions";
import { cn } from "@/lib/utils";
import { useAuthStore } from "@/stores/auth-store";
import { getVisibleNavSections } from "./navigation";

export function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const user = useAuthStore((state) => state.user);
  const logoutRemote = useAuthStore((state) => state.logoutRemote);
  const sections = getVisibleNavSections(user?.roles ?? []);
  const primaryRole = getPrimaryRole(user?.roles ?? []);

  async function handleLogout() {
    await logoutRemote();
    router.replace("/auth/login");
    router.refresh();
  }

  return (
    <div className="min-h-screen bg-background">
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-72 border-r border-border bg-card lg:flex lg:flex-col">
        <div className="flex h-16 items-center gap-3 border-b border-border px-5">
          <div className="flex h-9 w-9 items-center justify-center rounded-md bg-primary text-primary-foreground">
            <Activity className="h-5 w-5" />
          </div>
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold">StartupConnect</p>
            <p className="truncate text-xs text-muted-foreground">App workspace</p>
          </div>
        </div>

        <nav className="flex-1 overflow-y-auto px-3 py-4">
          {sections.map((section) => (
            <div key={section.title} className="mb-5">
              <p className="px-3 text-xs font-medium uppercase tracking-normal text-muted-foreground">{section.title}</p>
              <div className="mt-2 space-y-1">
                {section.items.map((item) => {
                  const Icon = item.icon;
                  const active = pathname === item.href || pathname.startsWith(`${item.href}/`);
                  return (
                    <Link
                      key={item.href}
                      className={cn(
                        "flex h-9 items-center gap-3 rounded-md px-3 text-sm font-medium transition-colors",
                        active
                          ? "bg-primary text-primary-foreground"
                          : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                      )}
                      href={item.href}
                    >
                      <Icon className="h-4 w-4" />
                      {item.title}
                    </Link>
                  );
                })}
              </div>
            </div>
          ))}
        </nav>

        <div className="border-t border-border p-4">
          <div className="rounded-md bg-muted p-3">
            <p className="truncate text-sm font-medium">{user?.fullName}</p>
            <p className="truncate text-xs text-muted-foreground">{user?.email}</p>
            <Badge className="mt-2" tone="default">
              {primaryRole}
            </Badge>
          </div>
          <Button className="mt-3 w-full" variant="outline" onClick={handleLogout}>
            <LogOut className="h-4 w-4" />
            Logout
          </Button>
        </div>
      </aside>

      <div className="lg:pl-72">
        <header className="sticky top-0 z-20 border-b border-border bg-card/95 backdrop-blur">
          <div className="flex h-16 items-center justify-between gap-3 px-4 sm:px-6 lg:px-8">
            <div className="flex min-w-0 items-center gap-3">
              <Menu className="h-5 w-5 text-muted-foreground lg:hidden" />
              <div className="min-w-0">
                <p className="truncate text-sm font-semibold">StartupConnect Console</p>
                <p className="truncate text-xs text-muted-foreground">{user?.email}</p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Badge tone="muted">{primaryRole}</Badge>
              <Button variant="outline" size="sm" onClick={handleLogout}>
                <LogOut className="h-4 w-4" />
                Logout
              </Button>
            </div>
          </div>
          <nav className="flex gap-2 overflow-x-auto border-t border-border px-4 py-2 lg:hidden">
            {sections.flatMap((section) =>
              section.items.map((item) => {
                const active = pathname === item.href || pathname.startsWith(`${item.href}/`);
                return (
                  <Link
                    key={item.href}
                    className={cn(
                      "whitespace-nowrap rounded-md px-3 py-1.5 text-xs font-medium",
                      active ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground"
                    )}
                    href={item.href}
                  >
                    {item.title}
                  </Link>
                );
              })
            )}
          </nav>
        </header>
        <main className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">{children}</main>
      </div>
    </div>
  );
}
