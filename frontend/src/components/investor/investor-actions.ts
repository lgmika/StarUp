import { InvestorInterestStatus } from "@/types/enums";

export function canWithdrawInterest(status: InvestorInterestStatus) {
  return (
    status !== InvestorInterestStatus.Accepted &&
    status !== InvestorInterestStatus.Rejected &&
    status !== InvestorInterestStatus.Withdrawn &&
    status !== InvestorInterestStatus.Closed
  );
}
