import { ProjectStatus } from "@/types/enums";

export function canEditProject(status: ProjectStatus) {
  return status !== ProjectStatus.Archived && status !== ProjectStatus.Closed;
}

export function canSubmitProject(status: ProjectStatus) {
  return status === ProjectStatus.Draft || status === ProjectStatus.NeedImprovement;
}

export function canCloseProject(status: ProjectStatus) {
  return status === ProjectStatus.Published || status === ProjectStatus.PendingReview || status === ProjectStatus.NeedImprovement;
}

export function canArchiveProject(status: ProjectStatus) {
  return status !== ProjectStatus.Archived;
}

export function getStatusActionHint(status: ProjectStatus) {
  if (canSubmitProject(status)) return "Ready to submit for moderator review.";
  if (status === ProjectStatus.PendingReview) return "Waiting for moderator review.";
  if (status === ProjectStatus.Published) return "Published and visible according to backend visibility rules.";
  if (status === ProjectStatus.Closed) return "Closed projects are no longer recruiting.";
  if (status === ProjectStatus.Archived) return "Archived project is removed from active management.";
  if (status === ProjectStatus.Rejected) return "Rejected by moderation.";
  if (status === ProjectStatus.Hidden) return "Hidden by moderation.";
  return "Review the latest status before taking action.";
}
