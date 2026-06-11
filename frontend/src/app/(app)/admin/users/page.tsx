"use client";

import { useEffect, useState } from "react";
import { RoleGuard } from "@/components/auth/role-guard";
import { MockNotice } from "@/components/admin/mock-notice";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody } from "@/components/ui/panel";
import { SystemRoles } from "@/lib/constants";
import { mockService } from "@/services";
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

  useEffect(() => {
    async function loadUsers() {
      setUsers(await mockService.getAdminUsers());
    }

    void loadUsers();
  }, []);

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold">Admin Users</h1>
        <p className="mt-2 text-sm text-muted-foreground">Prepared UI for future user-management endpoints.</p>
      </div>
      <MockNotice label="Admin users" />
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
              </div>
            </PanelBody>
          </Panel>
        ))}
      </div>
    </div>
  );
}
