import { ScrollText } from "lucide-react";
import { RoleGuard } from "@/components/auth/role-guard";
import { FeaturePlaceholder } from "@/components/common/feature-placeholder";
import { SystemRoles } from "@/lib/constants";

export default function ModeratorReportsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Moderator, SystemRoles.Admin]}>
      <FeaturePlaceholder
        icon={ScrollText}
        title="Moderator Reports"
        description="Reports do not have a complete user-facing API yet, so this remains a future mock-service surface."
        endpointHint="Uses mock service only until report endpoints exist."
      />
    </RoleGuard>
  );
}
