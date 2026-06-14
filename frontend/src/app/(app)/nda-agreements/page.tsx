"use client";

import { useEffect, useState } from "react";
import { FileSignature, ShieldCheck } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { PageHeader } from "@/components/workspace/page-header";
import { StatusBadge } from "@/components/workspace/status-badge";
import { ndaService } from "@/services/nda-service";
import type { NdaAgreementDto } from "@/types/nda";

export default function NdaAgreementsPage() {
  const [agreements, setAgreements] = useState<NdaAgreementDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadAgreements() {
      try {
        setAgreements(await ndaService.listMyAgreements());
      } catch (loadError) {
        setError(getApiErrorMessage(loadError));
      } finally {
        setIsLoading(false);
      }
    }

    void loadAgreements();
  }, []);

  if (isLoading) return <LoadingState label="Đang tải NDA agreements" />;

  return (
    <div className="space-y-5">
      <PageHeader title="NDA Agreements" description="Theo dõi trạng thái NDA, phiên bản đã ký và các thỏa thuận đang chờ xử lý." />
      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}
      <div className="grid gap-4 md:grid-cols-3">
        <Metric label="Accepted" value={agreements.length} />
        <Metric label="Pending" value={0} />
        <Metric label="Rejected" value={0} />
      </div>
      <Panel>
        <PanelHeader>
          <PanelTitle>Agreements</PanelTitle>
        </PanelHeader>
        <PanelBody className="space-y-3">
          {agreements.map((agreement) => (
            <div key={agreement.id} className="flex flex-col justify-between gap-3 rounded-md border border-border p-4 md:flex-row md:items-center">
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <ShieldCheck className="h-4 w-4 text-muted-foreground" />
                  <p className="font-medium">Project {agreement.projectId}</p>
                  <StatusBadge value="Accepted" />
                </div>
                <p className="mt-2 text-sm text-muted-foreground">
                  Version {agreement.versionNumber} · Accepted {agreement.acceptedAt}
                </p>
              </div>
              <Button variant="outline" size="sm">
                <FileSignature className="h-4 w-4" />
                Xem chi tiết
              </Button>
            </div>
          ))}
          {!error && agreements.length === 0 ? <p className="py-8 text-center text-sm text-muted-foreground">Chưa có NDA agreement nào.</p> : null}
        </PanelBody>
      </Panel>
    </div>
  );
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <Panel>
      <PanelBody>
        <p className="text-2xl font-semibold">{value}</p>
        <p className="mt-1 text-sm text-muted-foreground">{label}</p>
      </PanelBody>
    </Panel>
  );
}
