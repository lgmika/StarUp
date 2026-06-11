import { z } from "zod";

export const ndaTemplateSchema = z.object({
  name: z.string().trim().min(1, "Template name is required").max(160, "Template name is too long"),
  description: z.string().trim().min(1, "Description is required").max(500, "Description is too long"),
  initialContent: z.string().trim().min(20, "Initial content must be at least 20 characters"),
});

export const ndaTemplateVersionSchema = z.object({
  templateId: z.string().min(1, "Choose a template"),
  content: z.string().trim().min(20, "Version content must be at least 20 characters"),
});

export type NdaTemplateFormValues = z.infer<typeof ndaTemplateSchema>;
export type NdaTemplateVersionFormValues = z.infer<typeof ndaTemplateVersionSchema>;
