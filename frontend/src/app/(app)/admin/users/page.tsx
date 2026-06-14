"use client";

import { useEffect, useState } from "react";
import { UsersRound } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { SystemRoles } from "@/lib/constants";
import { adminService } from "@/services";
import type { AdminUserDto } from "@/types/admin";

export default function AdminUsersPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <AdminUsers />
    </RoleGuard>
  );
}

function AdminUsers() {
  const [users, setUsers] = useState<AdminUserDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function loadUsers() {
      const response = await adminService.listUsers({ pageSize: 50 });
      setUsers(response.items);
      setIsLoading(false);
    }

    void loadUsers();
  }, []);

  if (isLoading) return <LoadingState label="Loading admin users" />;

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold">Admin Users</h1>
        <p className="mt-2 text-sm text-muted-foreground">Review accounts, roles, verification, and enforcement status.</p>
      </div>
      {users.length === 0 ? <EmptyState icon={UsersRound} title="No users found" description="No admin-visible users were returned by the backend." /> : null}
      <div className="grid gap-3">
        {users.map((user) => (
          <Panel key={user.id}>
            <PanelBody className="flex flex-col justify-between gap-3 md:flex-row md:items-center">
              <div>
                <p className="font-semibold">{user.fullName}</p>
                <p className="text-sm text-muted-foreground">{user.email}</p>
              </div>
              <div className="flex flex-wrap gap-2">
                {user.roles.map((role) => <Badge key={role} tone="muted">{role}</Badge>)}
                <Badge tone={user.isEmailVerified ? "success" : "warning"}>{user.isEmailVerified ? "Verified" : "Unverified"}</Badge>
                <Badge tone={user.isSuspended ? "danger" : "success"}>{user.isSuspended ? "Suspended" : "Active"}</Badge>
                <Badge tone={user.isDeleted ? "danger" : "muted"}>{user.status}</Badge>
              </div>
            </PanelBody>
          </Panel>
        ))}
      </div>
    </div>
  );
}
