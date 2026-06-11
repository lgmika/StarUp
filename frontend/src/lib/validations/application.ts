import { z } from "zod";

export const applyProjectSchema = z.object({
  cvId: z.string().optional(),
  coverLetter: z.string().trim().min(20, "Cover letter must be at least 20 characters").max(4000, "Cover letter is too long"),
});

export const withdrawApplicationSchema = z.object({
  reason: z.string().trim().max(500, "Reason is too long").optional(),
});

export type ApplyProjectFormValues = z.infer<typeof applyProjectSchema>;
export type WithdrawApplicationFormValues = z.infer<typeof withdrawApplicationSchema>;
