import { z } from "zod";
import { ContactVisibility } from "@/types/enums";

const optionalUrl = z
  .string()
  .trim()
  .optional()
  .or(z.literal(""))
  .refine((value) => !value || /^https?:\/\/.+/i.test(value), "Use a valid HTTP or HTTPS URL");

export const profileSchema = z.object({
  headline: z.string().trim().min(1, "Headline is required").max(160, "Headline must be at most 160 characters"),
  bio: z.string().trim().min(1, "Bio is required").max(2000, "Bio must be at most 2000 characters"),
  location: z.string().trim().optional(),
  phoneNumber: z.string().trim().optional(),
  linkedInUrl: optionalUrl,
  gitHubUrl: optionalUrl,
  websiteUrl: optionalUrl,
  contactVisibility: z.nativeEnum(ContactVisibility),
});

export const addSkillSchema = z.object({
  skillId: z.string().min(1, "Choose a skill"),
  yearsOfExperience: z.coerce
    .number()
    .int("Years must be a whole number")
    .min(0, "Years must be at least 0")
    .max(60, "Years must be at most 60")
    .optional()
    .or(z.literal("")),
});

export const portfolioSchema = z.object({
  title: z.string().trim().min(1, "Portfolio title is required").max(160, "Title is too long"),
  url: z.string().trim().url("Use a valid URL").refine((value) => /^https?:\/\//i.test(value), "Use an HTTP or HTTPS URL"),
  description: z.string().trim().max(500, "Description is too long").optional(),
});

export const cvSchema = z.object({
  title: z.string().trim().min(1, "CV title is required").max(160, "CV title must be at most 160 characters"),
  summary: z.string().trim().max(1000, "Summary is too long").optional(),
  experienceJson: z.string().trim().optional(),
  educationJson: z.string().trim().optional(),
  isDefault: z.boolean(),
});

export type ProfileFormValues = z.infer<typeof profileSchema>;
export type AddSkillFormValues = z.infer<typeof addSkillSchema>;
export type PortfolioFormValues = z.infer<typeof portfolioSchema>;
export type CvFormValues = z.infer<typeof cvSchema>;
