"use client";

import { useEffect, useState } from "react";
import { CreditCard, PauseCircle, PlayCircle } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { PageHeader } from "@/components/workspace/page-header";
import { StatusBadge } from "@/components/workspace/status-badge";
import { subscriptionService } from "@/services/subscription-service";
import type { SubscriptionDto, SubscriptionPlanDto } from "@/types/subscription";

export default function BillingPage() {
  const [plans, setPlans] = useState<SubscriptionPlanDto[]>([]);
  const [current, setCurrent] = useState<SubscriptionDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadPlans() {
      try {
        const [nextPlans, nextCurrent] = await Promise.all([
          subscriptionService.listPlans(),
          subscriptionService.getCurrent().catch(() => null),
        ]);
        setPlans(nextPlans);
        setCurrent(nextCurrent);
      } catch (loadError) {
        setError(getApiErrorMessage(loadError));
      } finally {
        setIsLoading(false);
      }
    }

    void loadPlans();
  }, []);

  if (isLoading) return <LoadingState label="Đang tải gói sử dụng" />;

  return (
    <div className="space-y-5">
      <PageHeader title="Billing" description="Quản lý subscription, checkout, quota và trạng thái thanh toán." />
      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}
      <div className="grid gap-4 lg:grid-cols-3">
        {plans.map((plan) => (
          <Panel key={plan.id}>
            <PanelBody>
              <div className="flex items-start justify-between gap-3">
                <CreditCard className="h-5 w-5 text-muted-foreground" />
                <StatusBadge value={current?.planId === plan.id ? "Current" : "Available"} />
              </div>
              <h2 className="mt-4 text-lg font-semibold">{plan.name}</h2>
              <p className="mt-2 text-2xl font-semibold">{formatPrice(plan.monthlyPrice, plan.currency)}</p>
              <p className="mt-2 text-sm text-muted-foreground">{plan.description}</p>
              <ul className="mt-4 space-y-2 text-sm text-muted-foreground">
                {plan.quotas.map((quota) => <li key={quota.resourceKey}>{quota.resourceKey}: {quota.limit}</li>)}
              </ul>
              <div className="mt-5 flex gap-2">
                <Button variant={current?.planId === plan.id ? "outline" : "primary"} onClick={() => void handleCheckout(plan.id)}>
                  {current?.planId === plan.id ? "Đang dùng" : "Checkout"}
                </Button>
                {current?.planId === plan.id ? (
                  <Button variant="ghost" onClick={() => void handleCancel()}><PauseCircle className="h-4 w-4" />Hủy</Button>
                ) : (
                  <Button variant="ghost" onClick={() => void handleResume()}><PlayCircle className="h-4 w-4" />Resume</Button>
                )}
              </div>
            </PanelBody>
          </Panel>
        ))}
      </div>
    </div>
  );

  async function handleCheckout(planId: string) {
    try {
      const result = await subscriptionService.checkout({
        planId,
        successUrl: `${window.location.origin}/billing`,
        cancelUrl: `${window.location.origin}/billing`,
      });
      window.location.href = result.checkoutUrl;
    } catch (checkoutError) {
      setError(getApiErrorMessage(checkoutError));
    }
  }

  async function handleCancel() {
    try {
      setCurrent(await subscriptionService.cancel());
    } catch (cancelError) {
      setError(getApiErrorMessage(cancelError));
    }
  }

  async function handleResume() {
    try {
      setCurrent(await subscriptionService.resume());
    } catch (resumeError) {
      setError(getApiErrorMessage(resumeError));
    }
  }
}

function formatPrice(value: number, currency: string) {
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency }).format(value);
}
