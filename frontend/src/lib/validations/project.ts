import { z } from "zod";
import { ProjectStage, ProjectVisibility } from "@/types/enums";

export const projectFormSchema = z.object({
  title: z.string().trim().min(1, "Title is required").max(160, "Title is too long"),
  summary: z.string().trim().min(1, "Summary is required").max(500, "Summary is too long"),
  problem: z.string().trim().min(1, "Problem is required"),
  solution: z.string().trim().min(1, "Solution is required"),
  targetMarket: z.string().trim().optional(),
  businessModel: z.string().trim().optional(),
  fundingNeeds: z.string().trim().optional(),
  pitchDeckUrl: z
    .string()
    .trim()
    .optional()
    .or(z.literal(""))
    .refine((value) => !value || /^https?:\/\/.+/i.test(value), "Use a valid HTTP or HTTPS URL"),
  stage: z.nativeEnum(ProjectStage),
  visibility: z.nativeEnum(ProjectVisibility),
  isRecruiting: z.boolean(),
  requiredRolesText: z.string().trim().optional(),
  requiredSkillIds: z.array(z.string()),
});

export type ProjectFormValues = z.infer<typeof projectFormSchema>;
