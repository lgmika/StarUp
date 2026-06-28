"use client";

import { useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { FileCheck2, Loader2, Plus } from "lucide-react";
import { toast } from "sonner";
import { useForm } from "react-hook-form";
import type { UseFormRegisterReturn } from "react-hook-form";
import { RoleGuard } from "@/components/auth/role-guard";
import { FormField } from "@/components/auth/form-field";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import {
  ndaTemplateSchema,
  ndaTemplateVersionSchema,
  type NdaTemplateFormValues,
  type NdaTemplateVersionFormValues,
} from "@/lib/validations/nda";
import { ndaService } from "@/services";
import type { NdaTemplateDto } from "@/types/nda";

export default function AdminNdaTemplatesPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Admin]}>
      <NdaTemplates />
    </RoleGuard>
  );
}

function NdaTemplates() {
  const [templates, setTemplates] = useState<NdaTemplateDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const templateForm = useForm<NdaTemplateFormValues>({
    resolver: zodResolver(ndaTemplateSchema),
    defaultValues: { name: "", description: "", initialContent: "" },
  });
  const versionForm = useForm<NdaTemplateVersionFormValues>({
    resolver: zodResolver(ndaTemplateVersionSchema),
    defaultValues: { templateId: "", content: "" },
  });

  async function loadTemplates() {
    setIsLoading(true);
    try {
      setTemplates(await ndaService.listTemplates());
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  }

  async function createTemplate(values: NdaTemplateFormValues) {
    try {
      await ndaService.createTemplate(values);
      templateForm.reset({ name: "", description: "", initialContent: "" });
      await loadTemplates();
      toast.success("NDA template created.");
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    }
  }

  async function createVersion(values: NdaTemplateVersionFormValues) {
    try {
      await ndaService.createTemplateVersion(values.templateId, { content: values.content });
      versionForm.reset({ templateId: values.templateId, content: "" });
      await loadTemplates();
      toast.success("NDA template version created.");
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    }
  }

  useEffect(() => {
    void loadTemplates();
  }, []);

  if (isLoading) return <LoadingState label="Loading NDA templates" />;

  return (
    <div className="grid gap-6 xl:grid-cols-[420px_1fr]">
      <section className="space-y-5">
        <div>
          <h1 className="text-2xl font-semibold">NDA Templates</h1>
          <p className="mt-2 text-sm text-muted-foreground">This admin page uses real backend NDA template endpoints.</p>
        </div>
        <Panel>
          <PanelHeader>
            <PanelTitle>Create template</PanelTitle>
          </PanelHeader>
          <PanelBody>
            <form className="space-y-4" onSubmit={templateForm.handleSubmit(createTemplate)}>
              <FormField label="Name" error={templateForm.formState.errors.name} registration={templateForm.register("name")} />
              <FormField label="Description" error={templateForm.formState.errors.description} registration={templateForm.register("description")} />
              <TextArea label="Initial content" error={templateForm.formState.errors.initialContent?.message} registration={templateForm.register("initialContent")} />
              <Button type="submit" disabled={templateForm.formState.isSubmitting}>
                {templateForm.formState.isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
                Create template
              </Button>
            </form>
          </PanelBody>
        </Panel>
        <Panel>
          <PanelHeader>
            <PanelTitle>Add version</PanelTitle>
          </PanelHeader>
          <PanelBody>
            <form className="space-y-4" onSubmit={versionForm.handleSubmit(createVersion)}>
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Template</span>
                <select className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm" {...versionForm.register("templateId")}>
                  <option value="">Choose template</option>
                  {templates.map((template) => (
                    <option key={template.id} value={template.id}>{template.name}</option>
                  ))}
                </select>
                {versionForm.formState.errors.templateId ? <span className="text-xs text-destructive">{versionForm.formState.errors.templateId.message}</span> : null}
              </label>
              <TextArea label="Content" error={versionForm.formState.errors.content?.message} registration={versionForm.register("content")} />
              <Button type="submit" disabled={versionForm.formState.isSubmitting || templates.length === 0}>
                <FileCheck2 className="h-4 w-4" />
                Add version
              </Button>
            </form>
          </PanelBody>
        </Panel>
      </section>

      <section className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Templates</h2>
          <Badge tone="muted">{templates.length} items</Badge>
        </div>
        {templates.length === 0 ? (
          <Panel>
            <PanelBody className="py-12 text-center text-sm text-muted-foreground">No NDA templates returned yet.</PanelBody>
          </Panel>
        ) : null}
        {templates.map((template) => (
          <Panel key={template.id}>
            <PanelHeader className="flex flex-row items-center justify-between gap-3">
              <PanelTitle>{template.name}</PanelTitle>
              <Badge tone={template.isActive ? "success" : "muted"}>{template.isActive ? "Active" : "Inactive"}</Badge>
            </PanelHeader>
            <PanelBody className="space-y-3">
              <p className="text-sm text-muted-foreground">{template.description}</p>
              <div className="space-y-2">
                {template.versions.map((version) => (
                  <div key={version.id} className="rounded-md border border-border p-3">
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="text-sm font-semibold">Version {version.versionNumber}</p>
                      <Badge tone={version.isPublished ? "success" : "muted"}>{version.isPublished ? "Published" : "Draft"}</Badge>
                    </div>
                    <p className="mt-2 line-clamp-3 whitespace-pre-wrap text-sm text-muted-foreground">{version.content}</p>
                    <p className="mt-2 text-xs text-muted-foreground">{new Date(version.createdAt).toLocaleString()}</p>
                  </div>
                ))}
              </div>
            </PanelBody>
          </Panel>
        ))}
      </section>
    </div>
  );
}

function TextArea({ label, error, registration }: { label: string; error?: string; registration: UseFormRegisterReturn }) {
  return (
    <label className="block space-y-1.5 text-sm font-medium">
      <span>{label}</span>
      <textarea className="min-h-32 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring" {...registration} />
      {error ? <span className="text-xs text-destructive">{error}</span> : null}
    </label>
  );
}
