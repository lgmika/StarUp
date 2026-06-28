export type InterviewMeetingType = "Online" | "InPerson" | "Phone";
export type InterviewStatus = "Scheduled" | "Rescheduled" | "Completed" | "Cancelled" | "NoShow";

export interface InterviewParticipantDto {
  id: string;
  userId: string;
  email: string;
  fullName: string;
  isRequired: boolean;
}

export interface InterviewDto {
  id: string;
  applicationId: string;
  projectId: string;
  scheduledByUserId: string;
  startAt: string;
  endAt: string;
  timeZone: string;
  meetingType: InterviewMeetingType;
  meetingUrl?: string | null;
  location?: string | null;
  note?: string | null;
  status: InterviewStatus;
  cancellationReason?: string | null;
  createdAt: string;
  participants: InterviewParticipantDto[];
}

export interface UpdateInterviewRequest {
  startAt: string;
  endAt: string;
  timeZone: string;
  meetingType: InterviewMeetingType;
  meetingUrl?: string | null;
  location?: string | null;
  note?: string | null;
  participantUserIds?: string[];
}

export type CreateInterviewRequest = UpdateInterviewRequest;

export interface InterviewDecisionRequest {
  reason: string;
}
