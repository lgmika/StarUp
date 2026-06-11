import { RoleGuard } from "@/components/auth/role-guard";
import { MockNotice } from "@/components/admin/mock-notice";
import { Panel, PanelBody } from "@/components/ui/panel";
import { SystemRoles } from "@/lib/constants";

export default function AdminSettingsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <div className="space-y-5">
        <div>
          <h1 className="text-2xl font-semibold">Admin Settings</h1>
          <p className="mt-2 text-sm text-muted-foreground">Settings UI is intentionally minimal until backend-supported configuration exists.</p>
        </div>
        <MockNotice label="Admin settings" />
        <Panel>
          <PanelBody className="text-sm text-muted-foreground">
            No real settings endpoint is assumed in this phase.
          </PanelBody>
        </Panel>
      </div>
    </RoleGuard>
  );
}
