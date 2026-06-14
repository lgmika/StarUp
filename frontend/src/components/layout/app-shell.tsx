"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { FormEvent, useEffect, useState, type ReactNode } from "react";
import { Activity, Bell, ChevronDown, LogOut, Menu, Search, UserRound } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { getPrimaryRole } from "@/lib/permissions";
import { cn } from "@/lib/utils";
import { notificationService } from "@/services";
import { useAuthStore } from "@/stores/auth-store";
import type { NotificationDto } from "@/types/notification";
import { getVisibleNavSections } from "./navigation";

export function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const user = useAuthStore((state) => state.user);
  const logoutRemote = useAuthStore((state) => state.logoutRemote);
  const sections = getVisibleNavSections(user?.roles ?? []);
  const primaryRole = getPrimaryRole(user?.roles ?? []);
  const [query, setQuery] = useState("");
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [showNotifications, setShowNotifications] = useState(false);
  const [showUserMenu, setShowUserMenu] = useState(false);
  const unreadCount = notifications.filter((notification) => !notification.readAt).length;

  useEffect(() => {
    async function loadNotifications() {
      setNotifications(await notificationService.listNotifications());
    }

    void loadNotifications();
  }, []);

  function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const params = new URLSearchParams();
    if (query.trim()) params.set("keyword", query.trim());
    router.push(`/search${params.toString() ? `?${params.toString()}` : ""}`);
  }

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
          <div className="flex min-h-16 items-center justify-between gap-3 px-4 py-3 sm:px-6 lg:px-8">
            <div className="flex min-w-0 items-center gap-3">
              <Menu className="h-5 w-5 text-muted-foreground lg:hidden" />
              <div className="min-w-0">
                <p className="truncate text-sm font-semibold">StartupConnect Console</p>
                <p className="truncate text-xs text-muted-foreground">{user?.email}</p>
              </div>
            </div>

            <form className="hidden min-w-0 flex-1 justify-center md:flex" onSubmit={handleSearch}>
              <label className="sr-only" htmlFor="global-search">Global search</label>
              <div className="relative w-full max-w-xl">
                <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <input
                  id="global-search"
                  value={query}
                  onChange={(event) => setQuery(event.target.value)}
                  placeholder="Search projects, members, investors..."
                  className="h-10 w-full rounded-md border border-input bg-background pl-9 pr-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
                />
              </div>
            </form>

            <div className="relative flex items-center gap-2">
              <button
                type="button"
                aria-label="Open notifications"
                className="relative flex h-10 w-10 items-center justify-center rounded-md border border-border hover:bg-accent"
                onClick={() => setShowNotifications((value) => !value)}
              >
                <Bell className="h-4 w-4" />
                {unreadCount > 0 ? (
                  <span className="absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-semibold text-destructive-foreground">
                    {unreadCount}
                  </span>
                ) : null}
              </button>

              {showNotifications ? (
                <div className="absolute right-12 top-12 z-30 w-80 rounded-md border border-border bg-card p-3 shadow-lg">
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-semibold">Notifications</p>
                    <Link className="text-xs text-primary" href="/notifications" onClick={() => setShowNotifications(false)}>
                      View all
                    </Link>
                  </div>
                  <div className="mt-3 space-y-2">
                    {notifications.length === 0 ? (
                      <p className="rounded-md bg-muted p-3 text-xs text-muted-foreground">No notifications yet.</p>
                    ) : (
                      notifications.slice(0, 3).map((notification) => (
                        <div key={notification.id} className="rounded-md bg-muted p-3">
                          <p className="text-sm font-medium">{notification.title}</p>
                          <p className="mt-1 line-clamp-2 text-xs text-muted-foreground">{notification.message}</p>
                        </div>
                      ))
                    )}
                  </div>
                </div>
              ) : null}

              <div className="relative">
                <button
                  type="button"
                  className="flex h-10 items-center gap-2 rounded-md border border-border px-2 hover:bg-accent"
                  onClick={() => setShowUserMenu((value) => !value)}
                >
                  <UserRound className="h-4 w-4" />
                  <Badge tone="muted">{primaryRole}</Badge>
                  <ChevronDown className="h-4 w-4 text-muted-foreground" />
                </button>
                {showUserMenu ? (
                  <div className="absolute right-0 top-12 z-30 w-64 rounded-md border border-border bg-card p-3 shadow-lg">
                    <p className="truncate text-sm font-medium">{user?.fullName}</p>
                    <p className="truncate text-xs text-muted-foreground">{user?.email}</p>
                    <div className="mt-3 grid gap-1">
                      <Link className="rounded-md px-2 py-2 text-sm hover:bg-accent" href="/profile" onClick={() => setShowUserMenu(false)}>
                        Profile
                      </Link>
                      <Link className="rounded-md px-2 py-2 text-sm hover:bg-accent" href="/billing" onClick={() => setShowUserMenu(false)}>
                        Billing
                      </Link>
                      <button className="flex rounded-md px-2 py-2 text-sm text-destructive hover:bg-accent" type="button" onClick={handleLogout}>
                        <LogOut className="mr-2 h-4 w-4" />
                        Logout
                      </button>
                    </div>
                  </div>
                ) : null}
              </div>
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
