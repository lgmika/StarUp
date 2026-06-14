export type ConversationType = "Direct" | "Project" | "Application" | "InvestorInterest";

export interface CreateConversationRequest {
  type: ConversationType;
  participantUserIds: string[];
  projectId?: string;
  applicationId?: string;
  investorInterestId?: string;
  title?: string;
}

export interface ConversationParticipantDto {
  userId: string;
  email: string;
  fullName: string;
  lastReadAt?: string;
  isMuted: boolean;
}

export interface ConversationDto {
  id: string;
  type: ConversationType;
  projectId?: string;
  applicationId?: string;
  investorInterestId?: string;
  title?: string;
  createdAt: string;
  lastMessageAt?: string;
  participants: ConversationParticipantDto[];
}

export interface MessageAttachmentDto {
  id: string;
  fileId: string;
  originalFileName: string;
  contentType: string;
  sizeInBytes: number;
}

export interface MessageDto {
  id: string;
  conversationId: string;
  senderUserId: string;
  senderEmail: string;
  content: string;
  isDeleted: boolean;
  createdAt: string;
  attachments: MessageAttachmentDto[];
}

export interface MessageListResponse {
  items: MessageDto[];
  nextCursor?: string;
}

export interface SendMessageRequest {
  content: string;
  attachmentFileIds?: string[];
}
