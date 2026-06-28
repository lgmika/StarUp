"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Mail, ShieldCheck, UserRound } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { adminService } from "@/services/admin-service";

export default function AdminUserDetailPage() { return <RoleGuard allowedRoles={[SystemRoles.Admin]}><Detail /></RoleGuard>; }
function Detail() {
  const { id } = useParams<{ id: string }>();
  const query = useQuery({ queryKey: ["admin-user", id], queryFn: () => adminService.getUser(id) });
  if (query.isLoading) return <LoadingState label="Loading user detail" />;
  if (query.error || !query.data) return <p className="rounded-md bg-destructive/5 p-4 text-sm text-destructive">{getApiErrorMessage(query.error)}</p>;
  const user = query.data;
  return <div className="space-y-5"><Link href="/admin/users" className="inline-flex items-center gap-2 text-sm font-medium text-primary"><ArrowLeft className="h-4 w-4" />Back to users</Link><Panel><PanelBody className="flex flex-col gap-4 md:flex-row md:items-start"><div className="flex h-14 w-14 items-center justify-center rounded-md bg-primary/10 text-primary"><UserRound className="h-7 w-7" /></div><div className="flex-1"><h1 className="text-2xl font-semibold">{user.fullName}</h1><p className="mt-1 flex items-center gap-2 text-sm text-muted-foreground"><Mail className="h-4 w-4" />{user.email}</p><div className="mt-4 flex flex-wrap gap-2"><Badge tone={user.isEmailVerified ? "success" : "warning"}>{user.isEmailVerified ? "Verified" : "Unverified"}</Badge><Badge tone={user.isSuspended ? "danger" : "success"}>{user.status}</Badge>{user.bannedAt ? <Badge tone="danger">Banned</Badge> : null}</div></div></PanelBody></Panel>
    <div className="grid gap-4 lg:grid-cols-2"><Panel><PanelHeader><PanelTitle>Roles</PanelTitle></PanelHeader><PanelBody className="flex flex-wrap gap-2">{user.roles.map((role) => <Badge key={role}><ShieldCheck className="mr-1 h-3.5 w-3.5" />{role}</Badge>)}</PanelBody></Panel><Panel><PanelHeader><PanelTitle>Account timeline</PanelTitle></PanelHeader><PanelBody className="space-y-3 text-sm"><p className="flex items-center justify-between gap-3"><span className="text-muted-foreground">Created</span><span>{new Date(user.createdAt).toLocaleString()}</span></p><p className="flex items-center justify-between gap-3"><span className="text-muted-foreground">Last login</span><span>{user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : "Never"}</span></p>{user.suspensionReason ? <p className="rounded-md bg-amber-50 p-3 text-amber-800">{user.suspensionReason}</p> : null}{user.banReason ? <p className="rounded-md bg-red-50 p-3 text-red-800">{user.banReason}</p> : null}</PanelBody></Panel></div>
  </div>;
}
