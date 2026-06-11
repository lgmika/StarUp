import { UsersRound } from "lucide-react";
import { FeaturePlaceholder } from "@/components/common/feature-placeholder";

export default function JoinedProjectsPage() {
  return (
    <FeaturePlaceholder
      icon={UsersRound}
      title="Joined Projects"
      description="Membership views will be populated from backend project membership data in the project management phase."
      endpointHint="Uses /projects/me/joined."
    />
  );
}
