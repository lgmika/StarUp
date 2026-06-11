"use client";

import Link from "next/link";
import { BarChart3, Handshake, UserRound } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { Panel, PanelBody } from "@/components/ui/panel";
import { SystemRoles } from "@/lib/constants";

const links = [
  {
    href: "/investor/profile",
    title: "Investor profile",
    description: "Create or update your investor identity, ticket range, links, and investment focus.",
    icon: UserRound,
  },
  {
    href: "/investor/projects",
    title: "Investor projects",
    description: "Browse investor-visible projects, generate summaries, and express interest.",
    icon: BarChart3,
  },
  {
    href: "/investor/interests",
    title: "My interests",
    description: "Track interest status, founder responses, and withdraw active interests.",
    icon: Handshake,
  },
];

export default function InvestorPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Investor, SystemRoles.Admin]}>
      <div className="space-y-5">
        <div>
          <h1 className="text-2xl font-semibold">Investor Dashboard</h1>
          <p className="mt-2 text-sm text-muted-foreground">Investor workflows are connected to backend investor endpoints.</p>
        </div>
        <div className="grid gap-4 md:grid-cols-3">
          {links.map((item) => {
            const Icon = item.icon;
            return (
              <Link key={item.href} href={item.href}>
                <Panel className="h-full transition-colors hover:bg-accent">
                  <PanelBody>
                    <Icon className="h-5 w-5 text-muted-foreground" />
                    <h2 className="mt-4 text-sm font-semibold">{item.title}</h2>
                    <p className="mt-2 text-sm leading-6 text-muted-foreground">{item.description}</p>
                  </PanelBody>
                </Panel>
              </Link>
            );
          })}
        </div>
      </div>
    </RoleGuard>
  );
}
