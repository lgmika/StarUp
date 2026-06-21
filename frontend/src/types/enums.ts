export enum ProjectStatus {
  Draft = 'Draft',
  PendingReview = 'PendingReview',
  NeedImprovement = 'NeedImprovement',
  Published = 'Published',
  Rejected = 'Rejected',
  Hidden = 'Hidden',
  Closed = 'Closed',
  Archived = 'Archived',
}

export enum ProjectStage {
  Idea = 'Idea',
  Research = 'Research',
  Prototype = 'Prototype',
  MVP = 'MVP',
  Beta = 'Beta',
  Launched = 'Launched',
  Scaling = 'Scaling',
}

export enum ProjectVisibility {
  Public = 'Public',
  Limited = 'Limited',
  Private = 'Private',
  NdaRequired = 'NdaRequired',
  InvestorOnly = 'InvestorOnly',
}

export enum ApplicationStatus {
  Pending = 'Pending',
  Shortlisted = 'Shortlisted',
  Interviewing = 'Interviewing',
  AcceptedPendingNda = 'AcceptedPendingNda',
  Accepted = 'Accepted',
  Rejected = 'Rejected',
  Withdrawn = 'Withdrawn',
  Cancelled = 'Cancelled',
}

export enum InvestorInterestStatus {
  Pending = 'Pending',
  NeedMoreInfo = 'NeedMoreInfo',
  AcceptedPendingNda = 'AcceptedPendingNda',
  Accepted = 'Accepted',
  Rejected = 'Rejected',
  Withdrawn = 'Withdrawn',
  Closed = 'Closed',
}

export enum ModerationDecision {
  Approved = 'Approved',
  NeedImprovement = 'NeedImprovement',
  Rejected = 'Rejected',
  Hidden = 'Hidden',
  Restored = 'Restored',
}

export enum ContactVisibility {
  Private = 'Private',
  MembersOnly = 'MembersOnly',
  Public = 'Public',
}

export enum CvType {
  Internal = 'Internal',
  UploadedPdf = 'UploadedPdf',
}

export enum ProjectMemberRole {
  Founder = 'Founder',
  CoFounder = 'CoFounder',
  Member = 'Member',
  Advisor = 'Advisor',
  Investor = 'Investor',
  Viewer = 'Viewer',
}

export enum NotificationType {
  ProjectModeration = 'ProjectModeration',
  System = 'System',
  Application = 'Application',
  InvestorInterest = 'InvestorInterest',
  Chat = 'Chat',
  Report = 'Report',
  NDA = 'NDA',
  Interview = 'Interview',
  Billing = 'Billing',
}

export enum ReportStatus {
  Pending = 'Pending',
  Dismissed = 'Dismissed',
  Resolved = 'Resolved',
  Escalated = 'Escalated',
  Investigating = 'Investigating',
}

export enum ReportReasonCode {
  Spam = 'Spam',
  Scam = 'Scam',
  Harassment = 'Harassment',
  HateSpeech = 'HateSpeech',
  InappropriateContent = 'InappropriateContent',
  CopyrightViolation = 'CopyrightViolation',
  FakeInformation = 'FakeInformation',
  PrivacyViolation = 'PrivacyViolation',
  Other = 'Other',
}

export enum AIRequestType {
  ProjectSuggestions = 'ProjectSuggestions',
  ProjectReview = 'ProjectReview',
  InvestorSummary = 'InvestorSummary',
  ApplicationCoverLetter = 'ApplicationCoverLetter',
}
