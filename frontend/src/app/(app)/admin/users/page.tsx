"use client";

import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, Loader2, LockKeyhole, Search, ShieldPlus, UnlockKeyhole, UsersRound } from "lucide-react";
import { toast } from "sonner";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { queryKeys } from "@/lib/query-keys";
import { adminService } from "@/services";
import type { AdminUserDto } from "@/types/admin";

type UserAction = "suspend" | "unsuspend" | "ban" | "unban";

export default function AdminUsersPage() {
  return <RoleGuard allowedRoles={[SystemRoles.Admin]}><AdminUsers /></RoleGuard>;
}

function AdminUsers() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [pendingAction, setPendingAction] = useState<{ user: AdminUserDto; action: UserAction } | null>(null);
  const [reason, setReason] = useState("");
  const [roleByUser, setRoleByUser] = useState<Record<string, string>>({});
  const usersQuery = useQuery({ queryKey: [...queryKeys.admin, "users"], queryFn: () => adminService.listUsers({ pageSize: 100 }) });
  const rolesQuery = useQuery({ queryKey: [...queryKeys.admin, "roles"], queryFn: adminService.listRoles });
  const users = useMemo(() => {
    const term = search.trim().toLowerCase();
    return (usersQuery.data?.items ?? []).filter((user) => !term || user.fullName.toLowerCase().includes(term) || user.email.toLowerCase().includes(term));
  }, [search, usersQuery.data]);
  const refresh = () => queryClient.invalidateQueries({ queryKey: [...queryKeys.admin, "users"] });
  const actionMutation = useMutation({
    mutationFn: async ({ user, action }: { user: AdminUserDto; action: UserAction }) => {
      const request = { reason: reason.trim() || `Admin ${action}` };
      if (action === "suspend") return adminService.suspendUser(user.id, request);
      if (action === "unsuspend") return adminService.unsuspendUser(user.id, request);
      if (action === "ban") return adminService.banUser(user.id, request);
      return adminService.unbanUser(user.id, request);
    },
    onSuccess: () => { toast.success("User status updated."); setPendingAction(null); setReason(""); void refresh(); },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });
  const roleMutation = useMutation({
    mutationFn: ({ userId, roleCode, remove }: { userId: string; roleCode: string; remove?: boolean }) => remove ? adminService.removeRole(userId, roleCode) : adminService.addRole(userId, { roleCode }),
    onSuccess: () => { toast.success("User roles updated."); void refresh(); },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  if (usersQuery.isLoading) return <LoadingState label="Loading admin users" />;

  return <div className="space-y-5">
    <PageHeader title="Admin Users" description="Manage account access, enforcement status, and system roles." />
    <div className="relative max-w-md"><Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" /><Input className="pl-9" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search name or email" /></div>
    {usersQuery.error ? <p className="rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(usersQuery.error)}</p> : null}
    {!users.length ? <EmptyState icon={UsersRound} title="No users found" description="No users match the current search." /> : null}
    <div className="grid gap-3">{users.map((user) => {
      const isBanned = Boolean(user.bannedAt);
      const availableRoles = (rolesQuery.data ?? []).filter((role) => !user.roles.includes(role.code));
      return <Panel key={user.id}><PanelBody className="space-y-4">
        <div className="flex flex-col justify-between gap-3 md:flex-row md:items-start"><div><p className="font-semibold">{user.fullName}</p><p className="text-sm text-muted-foreground">{user.email}</p><p className="mt-1 text-xs text-muted-foreground">Created {new Date(user.createdAt).toLocaleDateString()}</p></div><div className="flex flex-wrap gap-2"><Badge tone={user.isEmailVerified ? "success" : "warning"}>{user.isEmailVerified ? "Verified" : "Unverified"}</Badge><Badge tone={user.isSuspended ? "danger" : "success"}>{user.isSuspended ? "Suspended" : "Active"}</Badge>{isBanned ? <Badge tone="danger">Banned</Badge> : null}</div></div>
        <div className="flex flex-wrap items-center gap-2">{user.roles.map((role) => <Button key={role} size="sm" variant="outline" disabled={roleMutation.isPending} title={`Remove ${role}`} onClick={() => roleMutation.mutate({ userId: user.id, roleCode: role, remove: true })}>{role} x</Button>)}
          <select className="h-8 rounded-md border border-input bg-background px-2 text-xs" value={roleByUser[user.id] ?? ""} onChange={(event) => setRoleByUser((current) => ({ ...current, [user.id]: event.target.value }))}><option value="">Select role</option>{availableRoles.map((role) => <option key={role.id} value={role.code}>{role.name}</option>)}</select>
          <Button size="sm" variant="outline" disabled={!roleByUser[user.id] || roleMutation.isPending} onClick={() => roleMutation.mutate({ userId: user.id, roleCode: roleByUser[user.id] })}>{roleMutation.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <ShieldPlus className="h-3.5 w-3.5" />}Assign</Button>
        </div>
        <div className="flex flex-wrap gap-2"><Button size="sm" variant="outline" onClick={() => setPendingAction({ user, action: user.isSuspended ? "unsuspend" : "suspend" })}>{user.isSuspended ? <UnlockKeyhole className="h-4 w-4" /> : <LockKeyhole className="h-4 w-4" />}{user.isSuspended ? "Unsuspend" : "Suspend"}</Button><Button size="sm" variant={isBanned ? "outline" : "danger"} onClick={() => setPendingAction({ user, action: isBanned ? "unban" : "ban" })}><Ban className="h-4 w-4" />{isBanned ? "Unban" : "Ban"}</Button></div>
      </PanelBody></Panel>;
    })}</div>
    <ConfirmDialog open={Boolean(pendingAction)} title={`${labelAction(pendingAction?.action)} user?`} description={`${labelAction(pendingAction?.action)} ${pendingAction?.user.fullName ?? "this user"}. This affects their platform access.`} confirmLabel={labelAction(pendingAction?.action)} isLoading={actionMutation.isPending} onClose={() => { setPendingAction(null); setReason(""); }} onConfirm={() => pendingAction && actionMutation.mutate(pendingAction)}><label className="block space-y-1.5 text-left text-sm font-medium"><span>Audit reason</span><Input value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Reason for this action" /></label></ConfirmDialog>
  </div>;
}

function labelAction(action?: UserAction) { return action ? `${action.charAt(0).toUpperCase()}${action.slice(1)}` : "Update"; }
