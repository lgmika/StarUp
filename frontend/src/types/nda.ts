export interface NdaTemplateDto {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  versions: NdaTemplateVersionDto[];
}

export interface NdaTemplateVersionDto {
  id: string;
  templateId: string;
  versionNumber: number;
  content: string;
  isPublished: boolean;
  createdAt: string;
}

export interface CreateNdaTemplateRequest {
  name: string;
  description: string;
  initialContent: string;
}

export interface CreateNdaTemplateVersionRequest {
  content: string;
}

export interface CurrentProjectNdaDto {
  projectId: string;
  requiresNda: boolean;
  templateId?: string;
  templateVersionId?: string;
  versionNumber?: number;
  content?: string;
  alreadyAccepted: boolean;
}

export interface NdaAgreementDto {
  id: string;
  projectId: string;
  userId: string;
  templateId: string;
  templateVersionId: string;
  versionNumber: number;
  acceptedAt: string;
  ipAddress?: string;
  userAgent?: string;
}
