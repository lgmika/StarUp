import Link from "next/link";
import { RoleGuard } from "@/components/auth/role-guard";
import { MockNotice } from "@/components/admin/mock-notice";
import { Panel, PanelBody } from "@/components/ui/panel";
import { SystemRoles } from "@/lib/constants";

export default function AdminProjectsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <div className="space-y-5">
        <div>
          <h1 className="text-2xl font-semibold">Admin Projects</h1>
          <p className="mt-2 text-sm text-muted-foreground">Admin-wide project endpoints are not complete yet; use moderation and project discovery where real APIs exist.</p>
        </div>
        <MockNotice label="Admin projects" />
        <Panel>
          <PanelBody className="flex flex-wrap gap-2">
            <Link className="inline-flex h-10 items-center rounded-md border border-border px-4 text-sm font-medium hover:bg-accent" href="/projects">
              Public discovery
            </Link>
            <Link className="inline-flex h-10 items-center rounded-md border border-border px-4 text-sm font-medium hover:bg-accent" href="/moderator/projects/pending">
              Moderation queue
            </Link>
          </PanelBody>
        </Panel>
      </div>
    </RoleGuard>
  );
}
