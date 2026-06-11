import { ApplicationStatus } from "@/types/enums";

export function canWithdrawApplication(status: ApplicationStatus) {
  return (
    status !== ApplicationStatus.Accepted &&
    status !== ApplicationStatus.Rejected &&
    status !== ApplicationStatus.Withdrawn &&
    status !== ApplicationStatus.Cancelled
  );
}

export function getApplicationStatusHint(status: ApplicationStatus) {
  if (status === ApplicationStatus.Pending) return "Your application is waiting for founder review.";
  if (status === ApplicationStatus.Shortlisted) return "Founder shortlisted your application.";
  if (status === ApplicationStatus.Interviewing) return "Founder moved your application to interviewing.";
  if (status === ApplicationStatus.AcceptedPendingNda) return "Accepted, but NDA acceptance is still pending.";
  if (status === ApplicationStatus.Accepted) return "Accepted. You may become a project member.";
  if (status === ApplicationStatus.Rejected) return "Founder rejected this application.";
  if (status === ApplicationStatus.Withdrawn) return "You withdrew this application.";
  return "Application is no longer active.";
}
