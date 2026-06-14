"use client";

import { useEffect, useState } from "react";
import { ShieldCheck } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { PageHeader } from "@/components/workspace/page-header";
import { SystemRoles } from "@/lib/constants";
import { adminService } from "@/services";
import type { AdminRoleDto } from "@/types/admin";

export default function AdminRolesPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <AdminRoles />
    </RoleGuard>
  );
}

function AdminRoles() {
  const [roles, setRoles] = useState<AdminRoleDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function loadRoles() {
      setRoles(await adminService.listRoles());
      setIsLoading(false);
    }

    void loadRoles();
  }, []);

  if (isLoading) return <LoadingState label="Loading roles" />;

  return (
    <div className="space-y-5">
      <PageHeader title="Admin Roles" description="Manage system roles exposed by the backend authorization model." />
      <Panel>
        <PanelHeader><PanelTitle>Roles</PanelTitle></PanelHeader>
        <PanelBody className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-border text-xs text-muted-foreground">
              <tr>
                <th className="py-2 font-medium">Role</th>
                <th className="py-2 font-medium">Code</th>
                <th className="py-2 font-medium">Description</th>
              </tr>
            </thead>
            <tbody>
              {roles.map((role) => (
                <tr key={role.id} className="border-b border-border">
                  <td className="py-3">
                    <div className="flex items-center gap-2 font-medium">
                      <ShieldCheck className="h-4 w-4 text-muted-foreground" />
                      {role.name}
                    </div>
                  </td>
                  <td className="py-3">{role.code}</td>
                  <td className="py-3 text-muted-foreground">{role.description ?? "No description"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </PanelBody>
      </Panel>
    </div>
  );
}
