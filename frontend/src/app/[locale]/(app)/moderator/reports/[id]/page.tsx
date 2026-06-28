"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { ReportDetailView } from "@/components/reports/report-detail-view";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { reportService } from "@/services/report-service";

export default function ModeratorReportDetailPage() { return <RoleGuard allowedRoles={[SystemRoles.Moderator, SystemRoles.Admin]}><Detail /></RoleGuard>; }
function Detail() {
  const { id } = useParams<{ id: string }>();
  const query = useQuery({ queryKey: ["moderator-report-detail", id], queryFn: () => reportService.getModeratorReport(id) });
  if (query.isLoading) return <LoadingState label="Loading moderation report" />;
  if (query.error || !query.data) return <p className="rounded-md bg-destructive/5 p-4 text-sm text-destructive">{getApiErrorMessage(query.error)}</p>;
  return <ReportDetailView detail={query.data} backHref="/moderator/reports" />;
}
