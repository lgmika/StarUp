"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Activity,
  BarChart3,
  BrainCircuit,
  ClipboardList,
  FileCheck2,
  FileClock,
  Handshake,
  ScrollText,
  Sparkles,
  UsersRound,
} from "lucide-react";
import { cn } from "@/lib/utils";

const items = [
  { segment: "dashboard", label: "Overview", icon: BarChart3 },
  { segment: "activity", label: "Activity", icon: Activity },
  { segment: "versions", label: "Versions", icon: FileClock },
  { segment: "members", label: "Members", icon: UsersRound },
  { segment: "recommended-members", label: "Recommended", icon: Sparkles },
  { segment: "applications", label: "Applications", icon: ClipboardList },
  { segment: "investor-interests", label: "Investor interests", icon: Handshake },
  { segment: "investor-summary", label: "Investor brief", icon: ScrollText },
  { segment: "ai-reviews", label: "AI reviews", icon: BrainCircuit },
  { segment: "nda-agreements", label: "NDA records", icon: FileCheck2 },
];

export function ProjectWorkspaceNav({ projectId }: { projectId: string }) {
  const pathname = usePathname();

  return (
    <nav
      aria-label="Project workspace"
      className="relative mb-6 flex gap-1.5 overflow-x-auto rounded-2xl border border-border/60 bg-card p-1.5 shadow-sm scrollbar-thin"
    >
      {items.map(({ segment, label, icon: Icon }) => {
        const href = `/projects/${projectId}/${segment}`;
        const isActive = pathname === href;

        return (
          <Link
            key={segment}
            href={href}
            className={cn(
              "group relative flex h-10 shrink-0 items-center gap-2.5 rounded-xl px-4 text-sm font-semibold transition-all duration-200",
              isActive
                ? "bg-primary text-primary-foreground shadow-md shadow-primary/25"
                : "text-muted-foreground hover:bg-muted hover:text-foreground"
            )}
          >
            <Icon
              className={cn(
                "h-4 w-4 transition-transform group-hover:scale-110",
                isActive ? "text-primary-foreground" : "text-muted-foreground"
              )}
            />
            {label}
          </Link>
        );
      })}
    </nav>
  );
}
