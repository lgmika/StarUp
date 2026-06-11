import { z } from "zod";

const optionalUrl = z
  .string()
  .trim()
  .optional()
  .or(z.literal(""))
  .refine((value) => !value || /^https?:\/\/.+/i.test(value), "Use a valid HTTP or HTTPS URL");

export const investorProfileSchema = z
  .object({
    displayName: z.string().trim().min(1, "Display name is required").max(160, "Display name is too long"),
    organizationName: z.string().trim().optional(),
    bio: z.string().trim().max(2000, "Bio is too long").optional(),
    investmentFocus: z.string().trim().max(1000, "Investment focus is too long").optional(),
    websiteUrl: optionalUrl,
    linkedInUrl: optionalUrl,
    minTicketSize: z.coerce.number().min(0, "Minimum ticket must be positive").optional().or(z.literal("")),
    maxTicketSize: z.coerce.number().min(0, "Maximum ticket must be positive").optional().or(z.literal("")),
  })
  .refine(
    (values) =>
      values.minTicketSize === "" ||
      values.maxTicketSize === "" ||
      values.minTicketSize == null ||
      values.maxTicketSize == null ||
      Number(values.minTicketSize) <= Number(values.maxTicketSize),
    {
      message: "Minimum ticket cannot exceed maximum ticket",
      path: ["maxTicketSize"],
    }
  );

export const investorInterestSchema = z.object({
  message: z.string().trim().min(20, "Message must be at least 20 characters").max(2000, "Message is too long"),
});

export type InvestorProfileFormValues = z.infer<typeof investorProfileSchema>;
export type InvestorInterestFormValues = z.infer<typeof investorInterestSchema>;
