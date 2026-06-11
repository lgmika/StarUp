export interface AIRecommendationDto {
  id: string;
  projectId: string;
  title: string;
  content: string;
  targetField: string;
  isApplied: boolean;
  createdAt: string;
}

export interface AIReviewDto {
  id: string;
  projectId: string;
  qualityScore: number;
  missingInformation: string[];
  riskFlags: string[];
  suggestions: string[];
  summary: string;
  createdAt: string;
}

export interface AITextResponse {
  content: string;
}

export interface ApplyAIRecommendationResponse {
  recommendationId: string;
  isApplied: boolean;
  appliedAt: string;
}
