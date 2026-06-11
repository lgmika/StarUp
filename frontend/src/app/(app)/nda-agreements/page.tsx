import { FileCheck2 } from "lucide-react";
import { FeaturePlaceholder } from "@/components/common/feature-placeholder";

export default function NdaAgreementsPage() {
  return (
    <FeaturePlaceholder
      icon={FileCheck2}
      title="NDA Agreements"
      description="Accepted NDA agreements will be shown only from backend-returned agreement data in the NDA phase."
      endpointHint="Uses /users/me/nda-agreements."
    />
  );
}
