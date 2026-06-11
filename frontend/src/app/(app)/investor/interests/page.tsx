"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { Handshake, Loader2, RefreshCw, RotateCcw } from "lucide-react";
import { toast } from "sonner";
import { RoleGuard } from "@/components/auth/role-guard";
import { canWithdrawInterest } from "@/components/investor/investor-actions";
import { InvestorInterestStatusBadge } from "@/components/investor/investor-interest-status-badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { investorService } from "@/services";
import type { InvestorInterestDto } from "@/types/investor";

export default function InvestorInterestsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Investor, SystemRoles.Admin]}>
      <InvestorInterests />
    </RoleGuard>
  );
}

function InvestorInterests() {
  const [interests, setInterests] = useState<InvestorInterestDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [runningId, setRunningId] = useState<string | null>(null);

  async function loadInterests() {
    setIsLoading(true);
    try {
      setInterests(await investorService.listMyInterests());
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  }

  async function withdraw(interest: InvestorInterestDto) {
    setRunningId(interest.id);
    try {
      const next = await investorService.withdrawInterest(interest.projectId, interest.id);
      setInterests((current) => current.map((item) => (item.id === interest.id ? next : item)));
      toast.success("Interest withdrawn.");
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    } finally {
      setRunningId(null);
    }
  }

  useEffect(() => {
    void loadInterests();
  }, []);

  return (
    <div className="space-y-5">
      <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
        <div>
          <h1 className="text-2xl font-semibold">My Investor Interests</h1>
          <p className="mt-2 text-sm text-muted-foreground">Track investor interest messages sent from your account.</p>
        </div>
        <Button variant="outline" onClick={() => void loadInterests()}>
          <RefreshCw className="h-4 w-4" />
          Refresh
        </Button>
      </div>
      {isLoading ? <Panel><PanelBody className="text-sm text-muted-foreground">Loading interests...</PanelBody></Panel> : null}
      {!isLoading && interests.length === 0 ? (
        <Panel>
          <PanelBody className="py-12 text-center">
            <Handshake className="mx-auto h-8 w-8 text-muted-foreground" />
            <h2 className="mt-4 text-base font-semibold">No investor interests yet</h2>
            <Link className="mt-4 inline-flex h-10 items-center rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground" href="/investor/projects">
              Discover investor projects
            </Link>
          </PanelBody>
        </Panel>
      ) : null}
      <div className="grid gap-3">
        {interests.map((interest) => (
          <Panel key={interest.id}>
            <PanelBody className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <h2 className="text-base font-semibold">{interest.projectTitle}</h2>
                  <InvestorInterestStatusBadge status={interest.status} />
                </div>
                <p className="mt-2 text-sm leading-6 text-muted-foreground">{interest.message}</p>
                {interest.founderResponse ? <p className="mt-2 text-sm text-muted-foreground">Founder: {interest.founderResponse}</p> : null}
                <p className="mt-2 text-xs text-muted-foreground">Sent {new Date(interest.createdAt).toLocaleString()}</p>
              </div>
              <div className="flex flex-wrap gap-2">
                <Link className="inline-flex h-9 items-center rounded-md border border-border px-3 text-sm font-medium hover:bg-accent" href={`/projects/${interest.projectId}`}>
                  Project
                </Link>
                <Button
                  variant="outline"
                  disabled={!canWithdrawInterest(interest.status) || runningId === interest.id}
                  onClick={() => void withdraw(interest)}
                >
                  {runningId === interest.id ? <Loader2 className="h-4 w-4 animate-spin" /> : <RotateCcw className="h-4 w-4" />}
                  Withdraw
                </Button>
              </div>
            </PanelBody>
          </Panel>
        ))}
      </div>
    </div>
  );
}
