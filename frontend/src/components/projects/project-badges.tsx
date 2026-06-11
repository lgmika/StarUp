import { Badge } from "@/components/ui/badge";
import {
  PROJECT_STAGE_COLORS,
  PROJECT_STAGE_LABELS,
  PROJECT_STATUS_COLORS,
  PROJECT_STATUS_LABELS,
  PROJECT_VISIBILITY_LABELS,
} from "@/lib/constants";
import { cn } from "@/lib/utils";
import type { ProjectStage, ProjectStatus, ProjectVisibility } from "@/types/enums";

const visibilityClasses: Record<ProjectVisibility, string> = {
  Public: "bg-emerald-100 text-emerald-700",
  Limited: "bg-sky-100 text-sky-700",
  Private: "bg-gray-100 text-gray-700",
  NdaRequired: "bg-amber-100 text-amber-700",
  InvestorOnly: "bg-indigo-100 text-indigo-700",
};

export function ProjectStatusBadge({ status }: { status: ProjectStatus }) {
  return <Badge className={cn("bg-transparent", PROJECT_STATUS_COLORS[status])}>{PROJECT_STATUS_LABELS[status]}</Badge>;
}

export function ProjectStageBadge({ stage }: { stage: ProjectStage }) {
  return <Badge className={cn("bg-transparent", PROJECT_STAGE_COLORS[stage])}>{PROJECT_STAGE_LABELS[stage]}</Badge>;
}

export function ProjectVisibilityBadge({ visibility }: { visibility: ProjectVisibility }) {
  return <Badge className={cn("bg-transparent", visibilityClasses[visibility])}>{PROJECT_VISIBILITY_LABELS[visibility]}</Badge>;
}
