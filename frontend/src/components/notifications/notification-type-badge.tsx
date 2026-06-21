import { Bell, BriefcaseBusiness, CreditCard, FileCheck2, Flag, MessageSquareText, ShieldCheck, UsersRound, Video } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { NotificationType } from "@/types/enums";

export function NotificationTypeBadge({ type }: { type: NotificationType }) {
  const config = {
    [NotificationType.ProjectModeration]: { label: "Project moderation", icon: ShieldCheck },
    [NotificationType.System]: { label: "System", icon: Bell },
    [NotificationType.Application]: { label: "Application", icon: BriefcaseBusiness },
    [NotificationType.InvestorInterest]: { label: "Investor interest", icon: UsersRound },
    [NotificationType.Chat]: { label: "Chat", icon: MessageSquareText },
    [NotificationType.Report]: { label: "Report", icon: Flag },
    [NotificationType.NDA]: { label: "NDA", icon: FileCheck2 },
    [NotificationType.Interview]: { label: "Interview", icon: Video },
    [NotificationType.Billing]: { label: "Billing", icon: CreditCard },
  }[type];
  const Icon = config.icon;

  return (
    <Badge tone={type === NotificationType.ProjectModeration ? "warning" : "muted"} className="gap-1.5">
      <Icon className="h-3 w-3" />
      {config.label}
    </Badge>
  );
}
