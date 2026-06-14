import { Badge } from "@/components/ui/badge";

export function StatusBadge({ value }: { value: string | number | null | undefined }) {
  const label = value === null || value === undefined ? "Unknown" : String(value);
  const tone =
    label.includes("Failed") || label.includes("Rejected") || label.includes("Hidden")
      ? "danger"
      : label.includes("Succeeded") || label.includes("Active") || label.includes("Verified") || label.includes("Accepted") || label.includes("Published")
        ? "success"
        : label.includes("Skipped") || label.includes("Pending") || label.includes("Invited")
          ? "warning"
          : "muted";

  return <Badge tone={tone}>{label}</Badge>;
}
