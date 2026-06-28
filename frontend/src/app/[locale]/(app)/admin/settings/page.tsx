"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Save, Settings } from "lucide-react";
import { toast } from "sonner";
import { RoleGuard } from "@/components/auth/role-guard";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { queryKeys } from "@/lib/query-keys";
import { adminService } from "@/services";

export default function AdminSettingsPage() {
  return <RoleGuard allowedRoles={[SystemRoles.Admin]}><AdminSettings /></RoleGuard>;
}

function AdminSettings() {
  const queryClient = useQueryClient();
  const [values, setValues] = useState<Record<string, string>>({});
  const settingsQuery = useQuery({ queryKey: [...queryKeys.admin, "settings"], queryFn: adminService.listSettings });
  const updateMutation = useMutation({
    mutationFn: ({ key, value }: { key: string; value: string }) => adminService.updateSetting(key, value, "Updated from admin console"),
    onSuccess: () => {
      toast.success("Setting updated.");
      void queryClient.invalidateQueries({ queryKey: [...queryKeys.admin, "settings"] });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  useEffect(() => {
    if (settingsQuery.data) setValues(Object.fromEntries(settingsQuery.data.map((setting) => [setting.key, setting.value])));
  }, [settingsQuery.data]);

  if (settingsQuery.isLoading) return <LoadingState label="Loading admin settings" />;

  return (
    <div className="space-y-5">
      <PageHeader title="Admin Settings" description="Manage backend system configuration with audit reasons and readonly enforcement." />
      {settingsQuery.error ? <p className="rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(settingsQuery.error)}</p> : null}
      <div className="grid gap-4 lg:grid-cols-2">
        {settingsQuery.data?.map((setting) => (
          <Panel key={setting.key}>
            <PanelHeader className="flex flex-row items-start justify-between gap-3">
              <div className="flex items-center gap-2"><Settings className="h-4 w-4 text-muted-foreground" /><PanelTitle>{setting.key}</PanelTitle></div>
              <Badge tone={setting.isReadonly ? "muted" : "success"}>{setting.isReadonly ? "Readonly" : setting.type}</Badge>
            </PanelHeader>
            <PanelBody>
              <p className="text-xs font-medium uppercase text-muted-foreground">{setting.group}</p>
              <div className="mt-3 flex gap-2">
                <Input value={values[setting.key] ?? setting.value} readOnly={setting.isReadonly} onChange={(event) => setValues((current) => ({ ...current, [setting.key]: event.target.value }))} />
                {!setting.isReadonly ? (
                  <Button size="icon" aria-label={`Save ${setting.key}`} disabled={updateMutation.isPending || values[setting.key] === setting.value} onClick={() => updateMutation.mutate({ key: setting.key, value: values[setting.key] ?? setting.value })}>
                    <Save className="h-4 w-4" />
                  </Button>
                ) : null}
              </div>
              <p className="mt-2 text-xs text-muted-foreground">Updated {setting.updatedAt ? new Date(setting.updatedAt).toLocaleString() : "from deployment defaults"}</p>
            </PanelBody>
          </Panel>
        ))}
      </div>
    </div>
  );
}
