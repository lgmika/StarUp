"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { BarChart3, FileCheck2, Handshake, KeyRound, UserRound } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { StatusBadge } from "@/components/workspace/status-badge";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { queryKeys } from "@/lib/query-keys";
import { investorService } from "@/services";

const links = [
  { href: "/investor/profile", title: "Investor profile", description: "Update investment identity and focus.", icon: UserRound },
  { href: "/investor/projects", title: "Discover projects", description: "Review investor-visible opportunities.", icon: BarChart3 },
  { href: "/investor/interests", title: "My interests", description: "Track founder responses and access.", icon: Handshake },
];

export default function InvestorPage() { return <RoleGuard allowedRoles={[SystemRoles.Investor, SystemRoles.Admin]}><InvestorDashboard /></RoleGuard>; }

function InvestorDashboard() {
  const dashboardQuery = useQuery({ queryKey: [...queryKeys.dashboard, "investor"], queryFn: investorService.getDashboard });
  if (dashboardQuery.isLoading) return <LoadingState label="Loading investor dashboard" />;
  const dashboard = dashboardQuery.data;
  return <div className="space-y-5"><div><h1 className="text-2xl font-semibold">Investor Dashboard</h1><p className="mt-2 text-sm text-muted-foreground">Portfolio access and interest metrics from the backend dashboard API.</p></div>
    {dashboardQuery.error ? <p className="rounded-md bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(dashboardQuery.error)}</p> : null}
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4"><Metric icon={Handshake} label="Interested projects" value={dashboard?.interestedProjects ?? 0} /><Metric icon={FileCheck2} label="NDA pending" value={dashboard?.ndaPending ?? 0} /><Metric icon={KeyRound} label="Accepted access" value={dashboard?.acceptedAccess ?? 0} /><Metric icon={BarChart3} label="Saved projects" value={dashboard?.savedProjects ?? 0} /></div>
    <div className="grid gap-4 lg:grid-cols-[1fr_360px]"><Panel><PanelHeader><PanelTitle>Interest status</PanelTitle></PanelHeader><PanelBody className="space-y-2">{dashboard?.interestStatus.length ? dashboard.interestStatus.map((item) => <div key={item.status} className="flex items-center justify-between rounded-md border border-border p-3"><StatusBadge value={item.status} /><span className="font-semibold">{item.count}</span></div>) : <p className="py-8 text-center text-sm text-muted-foreground">No investor interests yet.</p>}</PanelBody></Panel><div className="grid gap-3">{links.map((item) => { const Icon = item.icon; return <Link key={item.href} href={item.href}><Panel className="h-full hover:bg-accent"><PanelBody className="flex items-start gap-3"><Icon className="mt-0.5 h-5 w-5 text-muted-foreground" /><div><h2 className="text-sm font-semibold">{item.title}</h2><p className="mt-1 text-sm text-muted-foreground">{item.description}</p></div></PanelBody></Panel></Link>; })}</div></div>
  </div>;
}

function Metric({ icon: Icon, label, value }: { icon: typeof Handshake; label: string; value: number }) { return <Panel><PanelBody><Icon className="h-5 w-5 text-muted-foreground" /><p className="mt-4 text-2xl font-semibold">{value}</p><p className="mt-1 text-sm text-muted-foreground">{label}</p></PanelBody></Panel>; }
