import { Bell, ShieldCheck } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { NotificationType } from "@/types/enums";

export function NotificationTypeBadge({ type }: { type: NotificationType }) {
  const label = type === NotificationType.ProjectModeration ? "Project moderation" : "System";
  const Icon = type === NotificationType.ProjectModeration ? ShieldCheck : Bell;

  return (
    <Badge tone={type === NotificationType.ProjectModeration ? "warning" : "muted"} className="gap-1.5">
      <Icon className="h-3 w-3" />
      {label}
    </Badge>
  );
}
