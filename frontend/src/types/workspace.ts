import type { LucideIcon } from "lucide-react";

export interface FeedItem {
  id: string;
  actor: string;
  action: string;
  target: string;
  type: "Project" | "Application" | "NDA" | "Moderation";
  createdAt: string;
}

export interface RecommendationItem {
  id: string;
  title: string;
  category: "Project" | "Teammate" | "Investor";
  score: number;
  reason: string;
  status: string;
}

export interface TeamMemberItem {
  id: string;
  name: string;
  email: string;
  role: string;
  status: "Active" | "Invited" | "Pending";
}

export interface ConversationItem {
  id: string;
  title: string;
  lastMessage: string;
  unread: number;
  updatedAt: string;
}

export interface BillingPlanItem {
  id: string;
  name: string;
  price: string;
  status: "Current" | "Available";
  quotas: string[];
}

export interface BackgroundJobItem {
  id: string;
  jobName: string;
  status: "Succeeded" | "Failed" | "Skipped";
  startedAt: string;
  finishedAt: string;
  attempt: number;
  itemsProcessed: number;
  error?: string;
}

export interface SearchResultItem {
  id: string;
  title: string;
  type: "Project" | "Member" | "Investor" | "Suggestion";
  description: string;
  status: string;
}

export interface WorkspaceMetric {
  label: string;
  value: string;
  hint: string;
  icon: LucideIcon;
}

export interface JoinedProjectItem {
  id: string;
  title: string;
  role: string;
  stage: string;
  status: string;
  joinedAt: string;
  nextAction: string;
}

export interface NdaAgreementItem {
  id: string;
  projectTitle: string;
  versionNumber: number;
  status: "Accepted" | "Pending" | "Rejected";
  acceptedAt?: string;
  requestedAt: string;
}

export interface ModeratorReportItem {
  id: string;
  targetType: string;
  targetTitle: string;
  reporter: string;
  reason: string;
  status: "Pending" | "Investigating" | "Resolved" | "Dismissed" | "Escalated";
  assignedTo?: string;
  createdAt: string;
}

export interface AdminSettingItem {
  id: string;
  group: string;
  name: string;
  value: string;
  status: "Enabled" | "Disabled" | "Readonly";
}
