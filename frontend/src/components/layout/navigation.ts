import {
  BarChart3,
  Bell,
  BriefcaseBusiness,
  ClipboardList,
  FileCheck2,
  FileText,
  Folders,
  Handshake,
  LayoutDashboard,
  ListChecks,
  ScrollText,
  Search,
  Settings,
  ShieldCheck,
  SquarePen,
  UserRound,
  UsersRound,
  type LucideIcon,
} from "lucide-react";
import { SystemRoles, type SystemRole } from "@/lib/constants";

export interface NavItem {
  title: string;
  href: string;
  icon: LucideIcon;
  roles?: SystemRole[];
}

export interface NavSection {
  title: string;
  items: NavItem[];
}

export const navSections: NavSection[] = [
  {
    title: "Workspace",
    items: [
      { title: "Dashboard", href: "/dashboard", icon: LayoutDashboard },
      { title: "Discover projects", href: "/projects", icon: Search },
      { title: "Profile", href: "/profile", icon: UserRound },
      { title: "CVs", href: "/cvs", icon: FileText },
      { title: "Create project", href: "/projects/create", icon: SquarePen },
      { title: "My projects", href: "/projects/me/owned", icon: Folders },
      { title: "Joined projects", href: "/projects/me/joined", icon: UsersRound },
      { title: "Applications", href: "/applications", icon: ClipboardList },
      { title: "NDA agreements", href: "/nda-agreements", icon: FileCheck2 },
      { title: "Notifications", href: "/notifications", icon: Bell },
    ],
  },
  {
    title: "Investor",
    items: [
      {
        title: "Investor home",
        href: "/investor",
        icon: BriefcaseBusiness,
        roles: [SystemRoles.Investor, SystemRoles.Admin],
      },
      {
        title: "Investor profile",
        href: "/investor/profile",
        icon: UserRound,
        roles: [SystemRoles.Investor, SystemRoles.Admin],
      },
      {
        title: "Investor projects",
        href: "/investor/projects",
        icon: BarChart3,
        roles: [SystemRoles.Investor, SystemRoles.Admin],
      },
      {
        title: "Interests",
        href: "/investor/interests",
        icon: Handshake,
        roles: [SystemRoles.Investor, SystemRoles.Admin],
      },
    ],
  },
  {
    title: "Moderation",
    items: [
      {
        title: "Moderator",
        href: "/moderator",
        icon: ShieldCheck,
        roles: [SystemRoles.Moderator, SystemRoles.Admin],
      },
      {
        title: "Pending projects",
        href: "/moderator/projects/pending",
        icon: ListChecks,
        roles: [SystemRoles.Moderator, SystemRoles.Admin],
      },
      {
        title: "Reports",
        href: "/moderator/reports",
        icon: ScrollText,
        roles: [SystemRoles.Moderator, SystemRoles.Admin],
      },
    ],
  },
  {
    title: "Admin",
    items: [
      { title: "Admin", href: "/admin", icon: Settings, roles: [SystemRoles.Admin] },
      { title: "Users", href: "/admin/users", icon: UsersRound, roles: [SystemRoles.Admin] },
      { title: "Projects", href: "/admin/projects", icon: Folders, roles: [SystemRoles.Admin] },
      { title: "Reports", href: "/admin/reports", icon: ScrollText, roles: [SystemRoles.Admin] },
      { title: "Audit logs", href: "/admin/audit-logs", icon: ScrollText, roles: [SystemRoles.Admin] },
      { title: "NDA templates", href: "/admin/nda-templates", icon: FileCheck2, roles: [SystemRoles.Admin] },
      { title: "Settings", href: "/admin/settings", icon: Settings, roles: [SystemRoles.Admin] },
    ],
  },
];

export function getVisibleNavSections(roles: string[]) {
  return navSections
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => !item.roles || hasVisibleRole(roles, item.roles)),
    }))
    .filter((section) => section.items.length > 0);
}

function hasVisibleRole(userRoles: string[], itemRoles: SystemRole[]) {
  return itemRoles.some((role) => userRoles.includes(role));
}
