"use client";

import { useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Save, Trash2 } from "lucide-react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { queryKeys } from "@/lib/query-keys";
import { adminService } from "@/services";
import type { AdminSubscriptionPlanDto, AdminUsageQuotaDto } from "@/types/admin";

const planSchema = z.object({
  code: z.string().min(2).max(50),
  name: z.string().min(2).max(100),
  description: z.string().min(3).max(500),
  monthlyPrice: z.coerce.number().min(0),
  currency: z.string().length(3),
  isActive: z.boolean(),
  reason: z.string().max(300).optional(),
});
const quotaSchema = z.object({ resourceKey: z.string().min(2).max(120), limit: z.coerce.number().int().min(0), reason: z.string().max(300).optional() });
type PlanForm = z.infer<typeof planSchema>;
type QuotaForm = z.infer<typeof quotaSchema>;

const emptyPlan: PlanForm = { code: "", name: "", description: "", monthlyPrice: 0, currency: "USD", isActive: true, reason: "" };

export default function AdminSubscriptionsPage() {
  return <RoleGuard allowedRoles={[SystemRoles.Admin]}><AdminSubscriptions /></RoleGuard>;
}

function AdminSubscriptions() {
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<AdminSubscriptionPlanDto | null>(null);
  const [deleteQuota, setDeleteQuota] = useState<AdminUsageQuotaDto | null>(null);
  const plansQuery = useQuery({ queryKey: [...queryKeys.admin, "subscription-plans"], queryFn: adminService.listSubscriptionPlans });
  const planForm = useForm<PlanForm>({ resolver: zodResolver(planSchema), defaultValues: emptyPlan });
  const quotaForm = useForm<QuotaForm>({ resolver: zodResolver(quotaSchema), defaultValues: { resourceKey: "", limit: 0, reason: "" } });

  useEffect(() => {
    if (selected) planForm.reset({ code: selected.code, name: selected.name, description: selected.description, monthlyPrice: selected.monthlyPrice, currency: selected.currency, isActive: selected.isActive, reason: "" });
    else planForm.reset(emptyPlan);
  }, [selected, planForm]);

  useEffect(() => {
    if (!selected || !plansQuery.data) return;
    const refreshed = plansQuery.data.find((plan) => plan.id === selected.id);
    if (refreshed && refreshed !== selected) setSelected(refreshed);
  }, [plansQuery.data, selected]);

  const refresh = () => queryClient.invalidateQueries({ queryKey: [...queryKeys.admin, "subscription-plans"] });
  const planMutation = useMutation({
    mutationFn: (values: PlanForm) => selected ? adminService.updateSubscriptionPlan(selected.id, values) : adminService.createSubscriptionPlan(values),
    onSuccess: (plan) => { toast.success(selected ? "Plan updated." : "Plan created."); setSelected(plan); void refresh(); },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });
  const quotaMutation = useMutation({
    mutationFn: (values: QuotaForm) => {
      if (!selected) throw new Error("Select a plan first");
      return adminService.createUsageQuota(selected.id, values);
    },
    onSuccess: () => { toast.success("Quota created."); quotaForm.reset({ resourceKey: "", limit: 0, reason: "" }); void refresh(); },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });
  const deleteMutation = useMutation({
    mutationFn: () => selected && deleteQuota ? adminService.deleteUsageQuota(selected.id, deleteQuota.id, "Removed from admin console") : Promise.resolve(),
    onSuccess: () => { toast.success("Quota deleted."); setDeleteQuota(null); void refresh(); },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  if (plansQuery.isLoading) return <LoadingState label="Loading subscription plans" />;

  return (
    <div className="space-y-5">
      <PageHeader title="Plans & Quotas" description="Manage subscription plans and resource limits exposed by the backend admin API." actions={<Button variant="outline" onClick={() => setSelected(null)}><Plus className="h-4 w-4" />New plan</Button>} />
      {plansQuery.error ? <p className="rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(plansQuery.error)}</p> : null}
      <div className="grid gap-5 xl:grid-cols-[320px_1fr]">
        <Panel><PanelHeader><PanelTitle>Plans</PanelTitle></PanelHeader><PanelBody className="space-y-2">
          {plansQuery.data?.map((plan) => <button key={plan.id} type="button" onClick={() => setSelected(plan)} className={`w-full rounded-md border p-3 text-left ${selected?.id === plan.id ? "border-primary bg-primary/5" : "border-border hover:bg-accent"}`}><div className="flex items-center justify-between"><span className="font-medium">{plan.name}</span><Badge tone={plan.isActive ? "success" : "muted"}>{plan.isActive ? "Active" : "Inactive"}</Badge></div><p className="mt-1 text-xs text-muted-foreground">{plan.code} · {formatPrice(plan.monthlyPrice, plan.currency)}</p></button>)}
        </PanelBody></Panel>
        <div className="space-y-5">
          <Panel><PanelHeader><PanelTitle>{selected ? "Edit plan" : "Create plan"}</PanelTitle></PanelHeader><PanelBody>
            <form className="grid gap-4 md:grid-cols-2" onSubmit={planForm.handleSubmit((values) => planMutation.mutate(values))}>
              <Field label="Code" error={planForm.formState.errors.code?.message}><Input {...planForm.register("code")} /></Field><Field label="Name" error={planForm.formState.errors.name?.message}><Input {...planForm.register("name")} /></Field>
              <Field label="Monthly price" error={planForm.formState.errors.monthlyPrice?.message}><Input type="number" min={0} step="0.01" {...planForm.register("monthlyPrice")} /></Field><Field label="Currency" error={planForm.formState.errors.currency?.message}><Input maxLength={3} {...planForm.register("currency")} /></Field>
              <Field label="Description" error={planForm.formState.errors.description?.message} className="md:col-span-2"><Input {...planForm.register("description")} /></Field><Field label="Audit reason" error={planForm.formState.errors.reason?.message} className="md:col-span-2"><Input {...planForm.register("reason")} /></Field>
              <label className="flex items-center gap-2 text-sm"><input type="checkbox" className="h-4 w-4" {...planForm.register("isActive")} />Active plan</label>
              <div className="md:col-span-2"><Button type="submit" disabled={planMutation.isPending}><Save className="h-4 w-4" />{selected ? "Save plan" : "Create plan"}</Button></div>
            </form>
          </PanelBody></Panel>
          {selected ? <Panel><PanelHeader><PanelTitle>Usage quotas</PanelTitle></PanelHeader><PanelBody className="space-y-4">
            <form className="grid gap-3 md:grid-cols-[1fr_140px_1fr_auto]" onSubmit={quotaForm.handleSubmit((values) => quotaMutation.mutate(values))}><Input placeholder="Resource key" {...quotaForm.register("resourceKey")} /><Input type="number" min={0} placeholder="Limit" {...quotaForm.register("limit")} /><Input placeholder="Audit reason" {...quotaForm.register("reason")} /><Button type="submit" disabled={quotaMutation.isPending}><Plus className="h-4 w-4" />Add</Button></form>
            <div className="divide-y divide-border">{selected.quotas.map((quota) => <div key={quota.id} className="flex items-center justify-between py-3"><div><p className="text-sm font-medium">{quota.resourceKey}</p><p className="text-xs text-muted-foreground">Limit: {quota.limit}</p></div><Button size="icon" variant="danger" aria-label={`Delete ${quota.resourceKey}`} onClick={() => setDeleteQuota(quota)}><Trash2 className="h-4 w-4" /></Button></div>)}</div>
          </PanelBody></Panel> : null}
        </div>
      </div>
      <ConfirmDialog open={Boolean(deleteQuota)} title="Delete quota?" description={`Remove ${deleteQuota?.resourceKey ?? "this quota"} from the selected plan.`} confirmLabel="Delete quota" isLoading={deleteMutation.isPending} onClose={() => setDeleteQuota(null)} onConfirm={() => deleteMutation.mutate()} />
    </div>
  );
}

function Field({ label, error, children, className = "" }: { label: string; error?: string; children: React.ReactNode; className?: string }) { return <label className={`block space-y-1.5 text-sm font-medium ${className}`}><span>{label}</span>{children}{error ? <span className="text-xs text-destructive">{error}</span> : null}</label>; }
function formatPrice(value: number, currency: string) { return new Intl.NumberFormat("en", { style: "currency", currency }).format(value); }
