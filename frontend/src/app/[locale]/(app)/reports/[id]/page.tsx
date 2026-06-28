"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { LoadingState } from "@/components/common/loading-state";
import { ReportDetailView } from "@/components/reports/report-detail-view";
import { getApiErrorMessage } from "@/lib/api";
import { reportService } from "@/services/report-service";

export default function MyReportDetailPage() {
  const { id } = useParams<{ id: string }>();
  const query = useQuery({ queryKey: ["report-detail", id], queryFn: () => reportService.getMyReport(id) });
  if (query.isLoading) return <LoadingState label="Loading report detail" />;
  if (query.error || !query.data) return <p className="rounded-md bg-destructive/5 p-4 text-sm text-destructive">{getApiErrorMessage(query.error)}</p>;
  return <ReportDetailView detail={query.data} backHref="/reports" />;
}
