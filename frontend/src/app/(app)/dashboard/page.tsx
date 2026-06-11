"use client";

import Link from "next/link";
import { Bell, FileText, Folders, ShieldCheck } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getPrimaryRole } from "@/lib/permissions";
import { useAuthStore } from "@/stores/auth-store";

const quickLinks = [
  { title: "Complete profile", href: "/profile", icon: FileText },
  { title: "My projects", href: "/projects/me/owned", icon: Folders },
  { title: "Applications", href: "/applications", icon: ShieldCheck },
  { title: "Notifications", href: "/notifications", icon: Bell },
];

export default function DashboardPage() {
  const user = useAuthStore((state) => state.user);
  const primaryRole = getPrimaryRole(user?.roles ?? []);

  return (
    <div className="space-y-6">
      <section className="flex flex-col justify-between gap-4 rounded-lg border border-border bg-card p-5 shadow-sm sm:flex-row sm:items-center">
        <div>
          <p className="text-sm text-muted-foreground">Welcome back</p>
          <h1 className="mt-1 text-2xl font-semibold">{user?.fullName}</h1>
          <p className="mt-2 text-sm text-muted-foreground">Your authenticated workspace is ready for the next frontend phases.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Badge tone={user?.isEmailVerified ? "success" : "warning"}>
            {user?.isEmailVerified ? "Email verified" : "Email not verified"}
          </Badge>
          <Badge tone="default">{primaryRole}</Badge>
        </div>
      </section>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {quickLinks.map((item) => {
          const Icon = item.icon;
          return (
            <Link key={item.href} href={item.href} className="rounded-lg border border-border bg-card p-4 shadow-sm transition-colors hover:bg-accent">
              <Icon className="h-5 w-5 text-muted-foreground" />
              <p className="mt-3 text-sm font-semibold">{item.title}</p>
              <p className="mt-1 text-xs text-muted-foreground">Open section</p>
            </Link>
          );
        })}
      </div>

      <Panel>
        <PanelHeader>
          <PanelTitle>Phase 3 foundation</PanelTitle>
        </PanelHeader>
        <PanelBody className="grid gap-3 text-sm text-muted-foreground sm:grid-cols-3">
          <p>Protected routes load the current user through /auth/me before rendering private UI.</p>
          <p>Sidebar navigation is filtered by roles returned from the backend auth response.</p>
          <p>Role-only areas render a forbidden state when the current frontend role check fails.</p>
        </PanelBody>
      </Panel>
    </div>
  );
}
