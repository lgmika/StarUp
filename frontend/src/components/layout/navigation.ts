import {
  BarChart3, Bell, Bookmark, BriefcaseBusiness, CreditCard, ClipboardList,
  FileCheck2, FileText, Files, Folders, Handshake, LayoutDashboard, ListChecks,
  Mail, MessageSquareText, ScrollText, Search, Settings, ShieldCheck, Sparkles,
  SquarePen, UserRound, UsersRound, type LucideIcon,
} from "lucide-react";
import { SystemRoles, type SystemRole } from "@/lib/constants";
import { getPrimaryRole, getRoleHome } from "@/lib/permissions";

export interface NavItem { title: string; href: string; icon: LucideIcon }
export interface NavSection { title: string; items: NavItem[] }

const common = {
  dashboard: { title: "Dashboard", href: "/dashboard", icon: LayoutDashboard },
  discover: { title: "Discover projects", href: "/projects", icon: Search },
  search: { title: "Search", href: "/search", icon: Search },
  profile: { title: "Profile", href: "/profile", icon: UserRound },
  messages: { title: "Messages", href: "/messages", icon: MessageSquareText },
  notifications: { title: "Notifications", href: "/notifications", icon: Bell },
  nda: { title: "NDA agreements", href: "/nda-agreements", icon: FileCheck2 },
  reports: { title: "My reports", href: "/reports", icon: ScrollText },
};

const roleSections: Record<SystemRole, NavSection[]> = {
  [SystemRoles.Guest]: [],
  [SystemRoles.User]: [{ title: "Workspace", items: [common.dashboard, common.profile, common.notifications] }],
  [SystemRoles.VerifiedUser]: [
    { title: "Workspace", items: [common.dashboard, common.discover, common.search, { title: "Recommendations", href: "/recommendations", icon: Sparkles }, common.profile] },
    { title: "Career", items: [{ title: "CVs", href: "/cvs", icon: FileText }, { title: "Saved projects", href: "/projects/saved", icon: Bookmark }, { title: "Applications", href: "/applications", icon: ClipboardList }, { title: "Interviews", href: "/interviews", icon: BriefcaseBusiness }] },
    { title: "Communication", items: [common.messages, common.nda, common.reports, common.notifications] },
  ],
  [SystemRoles.Business]: [
    { title: "Founder", items: [{ title: "My projects", href: "/projects/me/owned", icon: Folders }, { title: "Create project", href: "/projects/create", icon: SquarePen }, { title: "Received applications", href: "/applications/received", icon: ListChecks }, { title: "Team", href: "/team", icon: UsersRound }] },
    { title: "Workspace", items: [common.discover, common.search, { title: "Recommendations", href: "/recommendations", icon: Sparkles }, common.profile, { title: "Files & CVs", href: "/files", icon: Files }] },
    { title: "Activity", items: [{ title: "Applications", href: "/applications", icon: ClipboardList }, { title: "Interviews", href: "/interviews", icon: BriefcaseBusiness }, common.messages, common.nda, common.notifications, { title: "Billing", href: "/billing", icon: CreditCard }] },
  ],
  [SystemRoles.Investor]: [
    { title: "Investor", items: [{ title: "Overview", href: "/investor", icon: LayoutDashboard }, { title: "Discover investments", href: "/investor/projects", icon: BarChart3 }, { title: "My interests", href: "/investor/interests", icon: Handshake }, { title: "Investor profile", href: "/investor/profile", icon: UserRound }] },
    { title: "Workspace", items: [common.discover, common.search, common.messages, common.nda, common.reports, common.notifications, { title: "Billing", href: "/billing", icon: CreditCard }] },
  ],
  [SystemRoles.Moderator]: [
    { title: "Moderation", items: [{ title: "Overview", href: "/moderator", icon: ShieldCheck }, { title: "Pending projects", href: "/moderator/projects/pending", icon: ListChecks }, { title: "Reports", href: "/moderator/reports", icon: ScrollText }] },
    { title: "Account", items: [common.profile, common.messages, common.notifications] },
  ],
  [SystemRoles.Admin]: [
    { title: "Administration", items: [{ title: "Overview", href: "/admin", icon: LayoutDashboard }, { title: "Users", href: "/admin/users", icon: UsersRound }, { title: "Roles", href: "/admin/roles", icon: ShieldCheck }, { title: "Projects", href: "/admin/projects", icon: Folders }, { title: "Reports", href: "/admin/reports", icon: ScrollText }] },
    { title: "Operations", items: [{ title: "Audit logs", href: "/admin/audit-logs", icon: ScrollText }, { title: "Background jobs", href: "/admin/background-jobs", icon: ListChecks }, { title: "Email outbox", href: "/admin/email-outbox", icon: Mail }, { title: "NDA templates", href: "/admin/nda-templates", icon: FileCheck2 }] },
    { title: "Configuration", items: [{ title: "Settings", href: "/admin/settings", icon: Settings }, { title: "Plans & quotas", href: "/admin/subscriptions", icon: CreditCard }, common.notifications] },
  ],
};

export function getVisibleNavSections(roles: string[]) {
  const primaryRole = getPrimaryRole(roles) as SystemRole;
  const sections = roleSections[primaryRole] ?? roleSections[SystemRoles.User];
  const home = getRoleHome(roles);
  return sections.map((section, index) => index === 0 ? { ...section, items: section.items.map((item, itemIndex) => itemIndex === 0 && item.title === "Dashboard" ? { ...item, href: home } : item) } : section);
}
