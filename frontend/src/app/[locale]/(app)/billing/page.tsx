"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CreditCard, PauseCircle, PlayCircle } from "lucide-react";
import { toast } from "sonner";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody } from "@/components/ui/panel";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { queryKeys } from "@/lib/query-keys";
import { subscriptionService } from "@/services/subscription-service";

export default function BillingPage() {
  const queryClient = useQueryClient();
  const plansQuery = useQuery({ queryKey: [...queryKeys.billing, "plans"], queryFn: subscriptionService.listPlans });
  const currentQuery = useQuery({ queryKey: [...queryKeys.billing, "current"], queryFn: subscriptionService.getCurrent, retry: false });
  const refresh = () => queryClient.invalidateQueries({ queryKey: queryKeys.billing });
  const checkoutMutation = useMutation({
    mutationFn: (planId: string) => subscriptionService.checkout({ planId, successUrl: `${window.location.origin}/billing`, cancelUrl: `${window.location.origin}/billing` }),
    onSuccess: (result) => {
      if (result.provider.toLowerCase().includes("mock") || !/^https?:\/\//.test(result.checkoutUrl)) {
        toast.info("Production payment provider is not configured yet.");
        return;
      }
      window.location.assign(result.checkoutUrl);
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });
  const subscriptionMutation = useMutation({
    mutationFn: (action: "cancel" | "resume") => action === "cancel" ? subscriptionService.cancel() : subscriptionService.resume(),
    onSuccess: (_, action) => { toast.success(action === "cancel" ? "Subscription cancelled." : "Subscription resumed."); void refresh(); },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  if (plansQuery.isLoading || currentQuery.isLoading) return <LoadingState label="Loading billing" />;
  const plans = plansQuery.data ?? [];
  const current = currentQuery.data;

  return <div className="space-y-5">
    <PageHeader title="Billing" description="Review plans, subscription status, and resource quotas." />
    {plansQuery.error ? <p className="rounded-md bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(plansQuery.error)}</p> : null}
    <div className="grid gap-4 lg:grid-cols-3">{plans.map((plan) => <Panel key={plan.id}><PanelBody><div className="flex items-start justify-between gap-3"><CreditCard className="h-5 w-5 text-muted-foreground" /><Badge tone={current?.planId === plan.id ? "success" : "muted"}>{current?.planId === plan.id ? "Current" : "Available"}</Badge></div><h2 className="mt-4 text-lg font-semibold">{plan.name}</h2><p className="mt-2 text-2xl font-semibold">{formatPrice(plan.monthlyPrice, plan.currency)}</p><p className="mt-2 text-sm text-muted-foreground">{plan.description}</p><ul className="mt-4 space-y-2 text-sm text-muted-foreground">{plan.quotas.map((quota) => <li key={quota.resourceKey} className="flex justify-between gap-3"><span>{quota.resourceKey}</span><span className="font-medium text-foreground">{quota.limit}</span></li>)}</ul><div className="mt-5 flex flex-wrap gap-2">{current?.planId === plan.id ? <><Button variant="outline" disabled>Current plan</Button><Button variant="ghost" disabled={subscriptionMutation.isPending} onClick={() => subscriptionMutation.mutate("cancel")}><PauseCircle className="h-4 w-4" />Cancel</Button></> : <Button disabled={checkoutMutation.isPending} onClick={() => checkoutMutation.mutate(plan.id)}>Checkout</Button>}{current?.planId === plan.id && current.status.toLowerCase().includes("cancel") ? <Button variant="outline" onClick={() => subscriptionMutation.mutate("resume")}><PlayCircle className="h-4 w-4" />Resume</Button> : null}</div></PanelBody></Panel>)}</div>
  </div>;
}

function formatPrice(value: number, currency: string) { return new Intl.NumberFormat("vi-VN", { style: "currency", currency }).format(value); }
