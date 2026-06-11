import { Badge } from "@/components/ui/badge";
import { INVESTOR_INTEREST_STATUS_COLORS, INVESTOR_INTEREST_STATUS_LABELS } from "@/lib/constants";
import { cn } from "@/lib/utils";
import type { InvestorInterestStatus } from "@/types/enums";

export function InvestorInterestStatusBadge({ status }: { status: InvestorInterestStatus }) {
  return <Badge className={cn("bg-transparent", INVESTOR_INTEREST_STATUS_COLORS[status])}>{INVESTOR_INTEREST_STATUS_LABELS[status]}</Badge>;
}
