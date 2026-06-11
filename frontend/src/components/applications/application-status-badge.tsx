import { Badge } from "@/components/ui/badge";
import { APPLICATION_STATUS_COLORS, APPLICATION_STATUS_LABELS } from "@/lib/constants";
import { cn } from "@/lib/utils";
import type { ApplicationStatus } from "@/types/enums";

export function ApplicationStatusBadge({ status }: { status: ApplicationStatus }) {
  return <Badge className={cn("bg-transparent", APPLICATION_STATUS_COLORS[status])}>{APPLICATION_STATUS_LABELS[status]}</Badge>;
}
