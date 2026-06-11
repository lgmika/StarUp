"use client";

import { useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { FileText, Loader2, Plus, Star, Trash2, Upload } from "lucide-react";
import { toast } from "sonner";
import { useForm } from "react-hook-form";
import { FormField } from "@/components/auth/form-field";
import { LoadingState } from "@/components/common/loading-state";
import { AuthMessage } from "@/components/auth/auth-message";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { cvSchema, type CvFormValues } from "@/lib/validations/profile";
import { profileService } from "@/services";
import type { CvDto } from "@/types/user";

const defaultCvValues: CvFormValues = {
  title: "",
  summary: "",
  experienceJson: "",
  educationJson: "",
  isDefault: false,
};

export default function CvsPage() {
  const [cvs, setCvs] = useState<CvDto[]>([]);
  const [editingCvId, setEditingCvId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const cvForm = useForm<CvFormValues>({
    resolver: zodResolver(cvSchema),
    defaultValues: defaultCvValues,
  });

  async function loadCvs() {
    setIsLoading(true);
    setError(null);
    try {
      setCvs(await profileService.listCvs());
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
    } finally {
      setIsLoading(false);
    }
  }

  async function saveCv(values: CvFormValues) {
    try {
      const request = normalizeCv(values);
      if (editingCvId) {
        await profileService.updateCv(editingCvId, request);
        toast.success("CV updated.");
      } else {
        await profileService.createCv(request);
        toast.success("CV created.");
      }
      setEditingCvId(null);
      cvForm.reset(defaultCvValues);
      await loadCvs();
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    }
  }

  async function deleteCv(cvId: string) {
    try {
      await profileService.deleteCv(cvId);
      await loadCvs();
      toast.success("CV deleted.");
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    }
  }

  async function setDefaultCv(cv: CvDto) {
    try {
      await profileService.updateCv(cv.id, {
        title: cv.title,
        summary: cv.summary,
        experienceJson: cv.experienceJson,
        educationJson: cv.educationJson,
        isDefault: true,
      });
      await loadCvs();
      toast.success("Default CV updated.");
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    }
  }

  async function uploadCv(file: File | undefined) {
    if (!file) return;
    if (file.type !== "application/pdf") {
      toast.error("Only PDF uploads are supported.");
      return;
    }

    setIsUploading(true);
    try {
      await profileService.uploadCv(file);
      await loadCvs();
      toast.success("PDF uploaded.");
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    } finally {
      setIsUploading(false);
    }
  }

  function startEdit(cv: CvDto) {
    setEditingCvId(cv.id);
    cvForm.reset({
      title: cv.title,
      summary: cv.summary ?? "",
      experienceJson: cv.experienceJson ?? "",
      educationJson: cv.educationJson ?? "",
      isDefault: cv.isDefault,
    });
  }

  function cancelEdit() {
    setEditingCvId(null);
    cvForm.reset(defaultCvValues);
  }

  useEffect(() => {
    void loadCvs();
  }, []);

  if (isLoading) return <LoadingState label="Loading CVs" />;

  return (
    <div className="grid gap-6 xl:grid-cols-[420px_1fr]">
      <section className="space-y-4">
        <div>
          <h1 className="text-2xl font-semibold">CV Management</h1>
          <p className="mt-2 text-sm text-muted-foreground">Create internal CV records or upload a PDF for application flows.</p>
        </div>
        {error ? <AuthMessage tone="error">{error}</AuthMessage> : null}

        <Panel>
          <PanelHeader>
            <PanelTitle>{editingCvId ? "Edit CV" : "Create CV"}</PanelTitle>
          </PanelHeader>
          <PanelBody>
            <form className="space-y-4" onSubmit={cvForm.handleSubmit(saveCv)}>
              <FormField label="Title" error={cvForm.formState.errors.title} registration={cvForm.register("title")} />
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Summary</span>
                <textarea
                  className="min-h-24 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  {...cvForm.register("summary")}
                />
              </label>
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Experience JSON</span>
                <textarea
                  className="min-h-20 w-full rounded-md border border-input bg-background px-3 py-2 font-mono text-xs outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  {...cvForm.register("experienceJson")}
                />
              </label>
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Education JSON</span>
                <textarea
                  className="min-h-20 w-full rounded-md border border-input bg-background px-3 py-2 font-mono text-xs outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  {...cvForm.register("educationJson")}
                />
              </label>
              <label className="flex items-center gap-2 text-sm font-medium">
                <input className="h-4 w-4 rounded border-input" type="checkbox" {...cvForm.register("isDefault")} />
                Set as default CV
              </label>
              <div className="flex flex-wrap gap-2">
                <Button type="submit" disabled={cvForm.formState.isSubmitting}>
                  {cvForm.formState.isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
                  {editingCvId ? "Save CV" : "Create CV"}
                </Button>
                {editingCvId ? (
                  <Button type="button" variant="outline" onClick={cancelEdit}>
                    Cancel
                  </Button>
                ) : null}
              </div>
            </form>
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader>
            <PanelTitle>Upload PDF</PanelTitle>
          </PanelHeader>
          <PanelBody>
            <label className="flex cursor-pointer flex-col items-center justify-center rounded-md border border-dashed border-border p-6 text-center transition-colors hover:bg-accent">
              {isUploading ? <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" /> : <Upload className="h-6 w-6 text-muted-foreground" />}
              <span className="mt-3 text-sm font-medium">Choose PDF file</span>
              <span className="mt-1 text-xs text-muted-foreground">Backend accepts PDF uploads up to its configured limit.</span>
              <input className="sr-only" type="file" accept="application/pdf" disabled={isUploading} onChange={(event) => void uploadCv(event.target.files?.[0])} />
            </label>
          </PanelBody>
        </Panel>
      </section>

      <section className="space-y-4">
        <div className="flex items-center justify-between gap-3">
          <h2 className="text-lg font-semibold">Your CVs</h2>
          <Badge tone="muted">{cvs.length} items</Badge>
        </div>

        {cvs.length ? (
          <div className="grid gap-3">
            {cvs.map((cv) => (
              <Panel key={cv.id}>
                <PanelBody className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <FileText className="h-4 w-4 text-muted-foreground" />
                      <h3 className="font-semibold">{cv.title}</h3>
                      <Badge tone={cv.type === "UploadedPdf" ? "warning" : "default"}>{cv.type}</Badge>
                      {cv.isDefault ? <Badge tone="success">Default</Badge> : null}
                    </div>
                    {cv.summary ? <p className="mt-2 text-sm leading-6 text-muted-foreground">{cv.summary}</p> : null}
                    {cv.fileName ? <p className="mt-2 text-xs text-muted-foreground">File: {cv.fileName}</p> : null}
                    <p className="mt-2 text-xs text-muted-foreground">Created {new Date(cv.createdAt).toLocaleDateString()}</p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button size="sm" variant="outline" onClick={() => startEdit(cv)}>
                      Edit
                    </Button>
                    <Button size="sm" variant="outline" disabled={cv.isDefault} onClick={() => void setDefaultCv(cv)}>
                      <Star className="h-4 w-4" />
                      Default
                    </Button>
                    <Button size="sm" variant="danger" onClick={() => void deleteCv(cv.id)}>
                      <Trash2 className="h-4 w-4" />
                      Delete
                    </Button>
                  </div>
                </PanelBody>
              </Panel>
            ))}
          </div>
        ) : (
          <Panel>
            <PanelBody className="py-12 text-center text-sm text-muted-foreground">
              No CVs yet. Create an internal CV or upload a PDF to use later in applications.
            </PanelBody>
          </Panel>
        )}
      </section>
    </div>
  );
}

function normalizeCv(values: CvFormValues) {
  return {
    title: values.title,
    summary: cleanOptional(values.summary),
    experienceJson: cleanOptional(values.experienceJson),
    educationJson: cleanOptional(values.educationJson),
    isDefault: values.isDefault,
  };
}

function cleanOptional(value?: string) {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}
