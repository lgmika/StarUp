import { z } from "zod";

export const moderationDecisionSchema = z.object({
  reason: z.string().trim().min(1, "Reason is required").max(1000, "Reason must be at most 1000 characters"),
});

export type ModerationDecisionFormValues = z.infer<typeof moderationDecisionSchema>;
