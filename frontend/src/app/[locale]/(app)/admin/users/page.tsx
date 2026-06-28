"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, Loader2, LockKeyhole, Search, Shield, ShieldPlus, UnlockKeyhole, UsersRound } from "lucide-react";
import { toast } from "sonner";
import { RoleGuard } from "@/components/auth/role-guard";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { DataTable } from "@/components/ui/data-table";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { queryKeys } from "@/lib/query-keys";
import { adminService } from "@/services";
import type { AdminUserDto } from "@/types/admin";
import { InlineError } from "@/components/ui/error-boundary";

type UserAction = "suspend" | "unsuspend" | "ban" | "unban";

export default function AdminUsersPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <AdminUsers />
    </RoleGuard>
  );
}

function AdminUsers() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [pendingAction, setPendingAction] = useState<{
    user: AdminUserDto;
    action: UserAction;
  } | null>(null);
  const [reason, setReason] = useState("");
  const [roleByUser, setRoleByUser] = useState<Record<string, string>>({});

  const usersQuery = useQuery({
    queryKey: [...queryKeys.admin, "users"],
    queryFn: () => adminService.listUsers({ pageSize: 100 }),
  });
  const rolesQuery = useQuery({
    queryKey: [...queryKeys.admin, "roles"],
    queryFn: adminService.listRoles,
  });

  const users = useMemo(() => {
    const term = search.trim().toLowerCase();
    return (usersQuery.data?.items ?? []).filter(
      (user) =>
        !term ||
        user.fullName.toLowerCase().includes(term) ||
        user.email.toLowerCase().includes(term)
    );
  }, [search, usersQuery.data]);

  const refresh = () =>
    queryClient.invalidateQueries({
      queryKey: [...queryKeys.admin, "users"],
    });

  const actionMutation = useMutation({
    mutationFn: async ({
      user,
      action,
    }: {
      user: AdminUserDto;
      action: UserAction;
    }) => {
      const request = { reason: reason.trim() || `Admin ${action}` };
      if (action === "suspend") return adminService.suspendUser(user.id, request);
      if (action === "unsuspend") return adminService.unsuspendUser(user.id, request);
      if (action === "ban") return adminService.banUser(user.id, request);
      return adminService.unbanUser(user.id, request);
    },
    onSuccess: () => {
      toast.success("User status updated.");
      setPendingAction(null);
      setReason("");
      void refresh();
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  const roleMutation = useMutation({
    mutationFn: ({
      userId,
      roleCode,
      remove,
    }: {
      userId: string;
      roleCode: string;
      remove?: boolean;
    }) =>
      remove
        ? adminService.removeRole(userId, roleCode)
        : adminService.addRole(userId, { roleCode }),
    onSuccess: () => {
      toast.success("User roles updated.");
      void refresh();
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  if (usersQuery.isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-20 rounded-2xl" />
        <Skeleton className="h-10 max-w-md rounded-xl" />
        <Skeleton className="h-64 rounded-2xl" />
      </div>
    );
  }

  const columns = [
    {
      key: "user",
      header: "User",
      render: (user: AdminUserDto) => (
        <div>
          <Link
            className="font-semibold hover:text-primary"
            href={`/admin/users/${user.id}`}
          >
            {user.fullName}
          </Link>
          <p className="text-xs text-muted-foreground">{user.email}</p>
        </div>
      ),
    },
    {
      key: "status",
      header: "Status",
      render: (user: AdminUserDto) => (
        <div className="flex flex-wrap gap-1.5">
          <Badge tone={user.isEmailVerified ? "success" : "warning"}>
            {user.isEmailVerified ? "Verified" : "Unverified"}
          </Badge>
          <Badge tone={user.isSuspended ? "danger" : "success"}>
            {user.isSuspended ? "Suspended" : "Active"}
          </Badge>
          {user.bannedAt ? <Badge tone="danger">Banned</Badge> : null}
        </div>
      ),
    },
    {
      key: "roles",
      header: "Roles",
      render: (user: AdminUserDto) => (
        <div className="flex flex-wrap gap-1">
          {user.roles.map((role) => (
            <button
              key={role}
              type="button"
              className="rounded-md bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive"
              disabled={roleMutation.isPending}
              title={`Remove ${role}`}
              onClick={() =>
                roleMutation.mutate({
                  userId: user.id,
                  roleCode: role,
                  remove: true,
                })
              }
            >
              {role} ×
            </button>
          ))}
        </div>
      ),
    },
    {
      key: "created",
      header: "Created",
      className: "whitespace-nowrap",
      render: (user: AdminUserDto) => (
        <span className="text-xs text-muted-foreground">
          {new Date(user.createdAt).toLocaleDateString()}
        </span>
      ),
    },
    {
      key: "actions",
      header: "Actions",
      className: "text-right",
      render: (user: AdminUserDto) => {
        const isBanned = Boolean(user.bannedAt);
        const availableRoles = (rolesQuery.data ?? []).filter(
          (role) => !user.roles.includes(role.code)
        );
        return (
          <div className="flex flex-wrap items-center justify-end gap-2">
            <select
              className="h-8 rounded-lg border border-input bg-background px-2 text-xs"
              value={roleByUser[user.id] ?? ""}
              onChange={(event) =>
                setRoleByUser((current) => ({
                  ...current,
                  [user.id]: event.target.value,
                }))
              }
            >
              <option value="">Add role</option>
              {availableRoles.map((role) => (
                <option key={role.id} value={role.code}>
                  {role.name}
                </option>
              ))}
            </select>
            {roleByUser[user.id] ? (
              <Button
                size="sm"
                variant="outline"
                disabled={roleMutation.isPending}
                onClick={() =>
                  roleMutation.mutate({
                    userId: user.id,
                    roleCode: roleByUser[user.id],
                  })
                }
              >
                {roleMutation.isPending ? (
                  <Loader2 className="h-3.5 w-3.5 animate-spin" />
                ) : (
                  <ShieldPlus className="h-3.5 w-3.5" />
                )}
              </Button>
            ) : null}
            <Button
              size="sm"
              variant="outline"
              onClick={() =>
                setPendingAction({
                  user,
                  action: user.isSuspended ? "unsuspend" : "suspend",
                })
              }
            >
              {user.isSuspended ? (
                <UnlockKeyhole className="h-3.5 w-3.5" />
              ) : (
                <LockKeyhole className="h-3.5 w-3.5" />
              )}
            </Button>
            <Button
              size="sm"
              variant={isBanned ? "outline" : "danger"}
              onClick={() =>
                setPendingAction({
                  user,
                  action: isBanned ? "unban" : "ban",
                })
              }
            >
              <Ban className="h-3.5 w-3.5" />
            </Button>
          </div>
        );
      },
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <section className="relative overflow-hidden rounded-2xl border border-border/60 bg-gradient-to-r from-blue-500/[0.04] via-card to-card p-6 shadow-sm">
        <div className="pointer-events-none absolute -right-16 -top-16 h-40 w-40 rounded-full bg-blue-500/5 blur-3xl" />
        <div className="relative flex items-start justify-between gap-4">
          <div>
            <div className="flex items-center gap-2">
              <Shield className="h-4 w-4 text-blue-600" />
              <p className="text-sm font-medium text-muted-foreground">
                Admin
              </p>
            </div>
            <h1 className="mt-2 text-2xl font-bold tracking-tight">
              User Management
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Manage account access, enforcement status, and system roles.
            </p>
          </div>
          <div className="flex items-center gap-2 rounded-xl bg-muted px-3 py-2 text-sm font-semibold">
            <UsersRound className="h-4 w-4" />
            {usersQuery.data?.items?.length ?? 0}
          </div>
        </div>
      </section>

      {/* Search */}
      <div className="relative max-w-md">
        <Search className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="rounded-xl pl-10"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search name or email..."
        />
      </div>

      {usersQuery.error ? (
        <InlineError message={getApiErrorMessage(usersQuery.error)} onRetry={() => void refresh()} />
      ) : null}

      {/* Data table */}
      <DataTable
        columns={columns}
        data={users}
        keyExtractor={(user) => user.id}
        isLoading={usersQuery.isLoading}
        emptyMessage="No users match the current search."
      />

      <ConfirmDialog
        open={Boolean(pendingAction)}
        title={`${labelAction(pendingAction?.action)} user?`}
        description={`${labelAction(pendingAction?.action)} ${pendingAction?.user.fullName ?? "this user"}. This affects their platform access.`}
        confirmLabel={labelAction(pendingAction?.action)}
        isLoading={actionMutation.isPending}
        onClose={() => {
          setPendingAction(null);
          setReason("");
        }}
        onConfirm={() =>
          pendingAction && actionMutation.mutate(pendingAction)
        }
      >
        <label className="block space-y-1.5 text-left text-sm font-medium">
          <span>Audit reason</span>
          <Input
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            placeholder="Reason for this action"
          />
        </label>
      </ConfirmDialog>
    </div>
  );
}

function labelAction(action?: UserAction) {
  return action
    ? `${action.charAt(0).toUpperCase()}${action.slice(1)}`
    : "Update";
}
