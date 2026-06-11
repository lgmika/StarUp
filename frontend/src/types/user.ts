import { ContactVisibility } from './enums';

export interface ProfileDto {
  userId: string;
  email: string;
  fullName: string;
  headline: string;
  bio: string;
  location?: string;
  phoneNumber?: string;
  linkedInUrl?: string;
  gitHubUrl?: string;
  websiteUrl?: string;
  contactVisibility: ContactVisibility;
  skills: SkillDto[];
  portfolios: PortfolioDto[];
}

export interface UpsertProfileRequest {
  headline: string;
  bio: string;
  location?: string;
  phoneNumber?: string;
  linkedInUrl?: string;
  gitHubUrl?: string;
  websiteUrl?: string;
  contactVisibility: ContactVisibility;
}

export interface SkillDto {
  id: string;
  name: string;
  yearsOfExperience?: number;
}

export interface AddUserSkillRequest {
  skillId: string;
  yearsOfExperience?: number;
}

export interface CvDto {
  id: string;
  title: string;
  summary?: string;
  experienceJson?: string;
  educationJson?: string;
  type: string;
  fileId?: string;
  fileName?: string;
  isDefault: boolean;
  createdAt: string;
}

export interface CreateCvRequest {
  title: string;
  summary?: string;
  experienceJson?: string;
  educationJson?: string;
  isDefault: boolean;
}

export interface UpdateCvRequest {
  title: string;
  summary?: string;
  experienceJson?: string;
  educationJson?: string;
  isDefault: boolean;
}

export interface PortfolioDto {
  id: string;
  title: string;
  url: string;
  description?: string;
  createdAt: string;
}

export interface CreatePortfolioRequest {
  title: string;
  url: string;
  description?: string;
}
