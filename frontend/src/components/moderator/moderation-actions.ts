import { ModerationDecision, ProjectStatus } from "@/types/enums";

export type ModerationAction = "approve" | "request-improvement" | "reject" | "hide" | "restore";

export const moderationActionLabels: Record<ModerationAction, string> = {
  approve: "Approve",
  "request-improvement": "Request improvement",
  reject: "Reject",
  hide: "Hide",
  restore: "Restore",
};

export const moderationActionDecision: Record<ModerationAction, ModerationDecision> = {
  approve: ModerationDecision.Approved,
  "request-improvement": ModerationDecision.NeedImprovement,
  reject: ModerationDecision.Rejected,
  hide: ModerationDecision.Hidden,
  restore: ModerationDecision.Restored,
};

export function canRunModerationAction(status: ProjectStatus, action: ModerationAction) {
  if (action === "approve") return status === ProjectStatus.PendingReview || status === ProjectStatus.NeedImprovement;
  if (action === "request-improvement") return status === ProjectStatus.PendingReview;
  if (action === "reject") return status === ProjectStatus.PendingReview;
  if (action === "hide") return status === ProjectStatus.Published;
  if (action === "restore") return status === ProjectStatus.Hidden;
  return false;
}
