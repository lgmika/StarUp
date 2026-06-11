import {
  ProjectStatus,
  ProjectStage,
  ProjectVisibility,
  ApplicationStatus,
  InvestorInterestStatus,
  ModerationDecision,
  ContactVisibility,
  ProjectMemberRole,
  ReportStatus,
} from '@/types/enums';

// System role codes
export const SystemRoles = {
  Guest: 'Guest',
  User: 'User',
  VerifiedUser: 'VerifiedUser',
  Business: 'Business',
  Investor: 'Investor',
  Moderator: 'Moderator',
  Admin: 'Admin',
} as const;

export type SystemRole = (typeof SystemRoles)[keyof typeof SystemRoles];

// Display labels for enums
export const PROJECT_STATUS_LABELS: Record<ProjectStatus, string> = {
  [ProjectStatus.Draft]: 'Draft',
  [ProjectStatus.PendingReview]: 'Pending Review',
  [ProjectStatus.NeedImprovement]: 'Need Improvement',
  [ProjectStatus.Published]: 'Published',
  [ProjectStatus.Rejected]: 'Rejected',
  [ProjectStatus.Hidden]: 'Hidden',
  [ProjectStatus.Closed]: 'Closed',
  [ProjectStatus.Archived]: 'Archived',
};

export const PROJECT_STATUS_COLORS: Record<ProjectStatus, string> = {
  [ProjectStatus.Draft]: 'bg-gray-100 text-gray-700',
  [ProjectStatus.PendingReview]: 'bg-blue-100 text-blue-700',
  [ProjectStatus.NeedImprovement]: 'bg-amber-100 text-amber-700',
  [ProjectStatus.Published]: 'bg-emerald-100 text-emerald-700',
  [ProjectStatus.Rejected]: 'bg-red-100 text-red-700',
  [ProjectStatus.Hidden]: 'bg-gray-100 text-gray-500',
  [ProjectStatus.Closed]: 'bg-gray-100 text-gray-600',
  [ProjectStatus.Archived]: 'bg-gray-100 text-gray-500',
};

export const PROJECT_STAGE_LABELS: Record<ProjectStage, string> = {
  [ProjectStage.Idea]: 'Idea',
  [ProjectStage.Research]: 'Research',
  [ProjectStage.Prototype]: 'Prototype',
  [ProjectStage.MVP]: 'MVP',
  [ProjectStage.Beta]: 'Beta',
  [ProjectStage.Launched]: 'Launched',
  [ProjectStage.Scaling]: 'Scaling',
};

export const PROJECT_STAGE_COLORS: Record<ProjectStage, string> = {
  [ProjectStage.Idea]: 'bg-purple-100 text-purple-700',
  [ProjectStage.Research]: 'bg-blue-100 text-blue-700',
  [ProjectStage.Prototype]: 'bg-cyan-100 text-cyan-700',
  [ProjectStage.MVP]: 'bg-teal-100 text-teal-700',
  [ProjectStage.Beta]: 'bg-amber-100 text-amber-700',
  [ProjectStage.Launched]: 'bg-emerald-100 text-emerald-700',
  [ProjectStage.Scaling]: 'bg-indigo-100 text-indigo-700',
};

export const PROJECT_VISIBILITY_LABELS: Record<ProjectVisibility, string> = {
  [ProjectVisibility.Public]: 'Public',
  [ProjectVisibility.Limited]: 'Limited',
  [ProjectVisibility.Private]: 'Private',
  [ProjectVisibility.NdaRequired]: 'NDA Required',
  [ProjectVisibility.InvestorOnly]: 'Investor Only',
};

export const APPLICATION_STATUS_LABELS: Record<ApplicationStatus, string> = {
  [ApplicationStatus.Pending]: 'Pending',
  [ApplicationStatus.Shortlisted]: 'Shortlisted',
  [ApplicationStatus.Interviewing]: 'Interviewing',
  [ApplicationStatus.AcceptedPendingNda]: 'Accepted (Pending NDA)',
  [ApplicationStatus.Accepted]: 'Accepted',
  [ApplicationStatus.Rejected]: 'Rejected',
  [ApplicationStatus.Withdrawn]: 'Withdrawn',
  [ApplicationStatus.Cancelled]: 'Cancelled',
};

export const APPLICATION_STATUS_COLORS: Record<ApplicationStatus, string> = {
  [ApplicationStatus.Pending]: 'bg-amber-100 text-amber-700',
  [ApplicationStatus.Shortlisted]: 'bg-blue-100 text-blue-700',
  [ApplicationStatus.Interviewing]: 'bg-purple-100 text-purple-700',
  [ApplicationStatus.AcceptedPendingNda]: 'bg-cyan-100 text-cyan-700',
  [ApplicationStatus.Accepted]: 'bg-emerald-100 text-emerald-700',
  [ApplicationStatus.Rejected]: 'bg-red-100 text-red-700',
  [ApplicationStatus.Withdrawn]: 'bg-gray-100 text-gray-600',
  [ApplicationStatus.Cancelled]: 'bg-gray-100 text-gray-500',
};

export const INVESTOR_INTEREST_STATUS_LABELS: Record<InvestorInterestStatus, string> = {
  [InvestorInterestStatus.Pending]: 'Pending',
  [InvestorInterestStatus.NeedMoreInfo]: 'More Info Needed',
  [InvestorInterestStatus.AcceptedPendingNda]: 'Accepted (Pending NDA)',
  [InvestorInterestStatus.Accepted]: 'Accepted',
  [InvestorInterestStatus.Rejected]: 'Rejected',
  [InvestorInterestStatus.Withdrawn]: 'Withdrawn',
  [InvestorInterestStatus.Closed]: 'Closed',
};

export const INVESTOR_INTEREST_STATUS_COLORS: Record<InvestorInterestStatus, string> = {
  [InvestorInterestStatus.Pending]: 'bg-amber-100 text-amber-700',
  [InvestorInterestStatus.NeedMoreInfo]: 'bg-blue-100 text-blue-700',
  [InvestorInterestStatus.AcceptedPendingNda]: 'bg-cyan-100 text-cyan-700',
  [InvestorInterestStatus.Accepted]: 'bg-emerald-100 text-emerald-700',
  [InvestorInterestStatus.Rejected]: 'bg-red-100 text-red-700',
  [InvestorInterestStatus.Withdrawn]: 'bg-gray-100 text-gray-600',
  [InvestorInterestStatus.Closed]: 'bg-gray-100 text-gray-500',
};

export const MODERATION_DECISION_LABELS: Record<ModerationDecision, string> = {
  [ModerationDecision.Approved]: 'Approved',
  [ModerationDecision.NeedImprovement]: 'Need Improvement',
  [ModerationDecision.Rejected]: 'Rejected',
  [ModerationDecision.Hidden]: 'Hidden',
  [ModerationDecision.Restored]: 'Restored',
};

export const CONTACT_VISIBILITY_LABELS: Record<ContactVisibility, string> = {
  [ContactVisibility.Private]: 'Private',
  [ContactVisibility.MembersOnly]: 'Members Only',
  [ContactVisibility.Public]: 'Public',
};

export const PROJECT_MEMBER_ROLE_LABELS: Record<ProjectMemberRole, string> = {
  [ProjectMemberRole.Founder]: 'Founder',
  [ProjectMemberRole.CoFounder]: 'Co-Founder',
  [ProjectMemberRole.Member]: 'Member',
  [ProjectMemberRole.Advisor]: 'Advisor',
  [ProjectMemberRole.Investor]: 'Investor',
  [ProjectMemberRole.Viewer]: 'Viewer',
};

export const REPORT_STATUS_LABELS: Record<ReportStatus, string> = {
  [ReportStatus.Pending]: 'Pending',
  [ReportStatus.Dismissed]: 'Dismissed',
  [ReportStatus.Resolved]: 'Resolved',
  [ReportStatus.Escalated]: 'Escalated',
};
