"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { FileText, Star, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { InlineError } from "@/components/ui/error-boundary";
import { getApiErrorMessage } from "@/lib/api";
import { profileService } from "@/services";
import type { CvDto } from "@/types/user";

import { CvForm } from "@/components/cv/cv-form";
import { CvUpload } from "@/components/cv/cv-upload";

export default function CvsPage() {
  const queryClient = useQueryClient();
  const [editingCv, setEditingCv] = useState<CvDto | null>(null);

  const cvsQuery = useQuery({
    queryKey: ["cvs"],
    queryFn: () => profileService.listCvs(),
  });

  const deleteMutation = useMutation({
    mutationFn: (cvId: string) => profileService.deleteCv(cvId),
    onSuccess: () => {
      toast.success("CV deleted.");
      void queryClient.invalidateQueries({ queryKey: ["cvs"] });
      if (editingCv) setEditingCv(null);
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  const setDefaultMutation = useMutation({
    mutationFn: (cv: CvDto) =>
      profileService.updateCv(cv.id, {
        title: cv.title,
        summary: cv.summary,
        experienceJson: cv.experienceJson,
        educationJson: cv.educationJson,
        isDefault: true,
      }),
    onSuccess: () => {
      toast.success("Default CV updated.");
      void queryClient.invalidateQueries({ queryKey: ["cvs"] });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  if (cvsQuery.isLoading) return <LoadingState label="Loading CVs" />;

  const cvs = cvsQuery.data ?? [];

  return (
    <div className="space-y-6">
      {/* Header */}
      <section className="relative overflow-hidden rounded-2xl border border-border/60 bg-gradient-to-r from-primary/[0.04] via-card to-card p-6 shadow-sm">
        <div className="pointer-events-none absolute -right-16 -top-16 h-40 w-40 rounded-full bg-primary/5 blur-3xl" />
        <div className="relative">
          <div className="flex items-center gap-2">
            <FileText className="h-4 w-4 text-primary" />
            <p className="text-sm font-medium text-muted-foreground">Account</p>
          </div>
          <h1 className="mt-2 text-2xl font-bold tracking-tight">CV Management</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Create internal CV records or upload a PDF for application flows.
          </p>
        </div>
      </section>

      {cvsQuery.error ? (
        <InlineError
          message={getApiErrorMessage(cvsQuery.error)}
          onRetry={() => void cvsQuery.refetch()}
        />
      ) : null}

      <div className="grid gap-6 xl:grid-cols-[420px_1fr]">
        {/* Left Column: Forms */}
        <section className="space-y-6">
          <CvForm editingCv={editingCv} onCancelEdit={() => setEditingCv(null)} />
          <CvUpload />
        </section>

        {/* Right Column: List */}
        <section className="space-y-4">
          <div className="flex items-center justify-between gap-3 px-2">
            <h2 className="text-lg font-bold">Your CVs</h2>
            <Badge tone="muted" className="bg-muted text-muted-foreground">
              {cvs.length} items
            </Badge>
          </div>

          {cvs.length ? (
            <div className="grid gap-3">
              {cvs.map((cv) => (
                <div
                  key={cv.id}
                  className="group rounded-2xl border border-border/60 bg-card p-5 shadow-sm transition-all hover:shadow-md"
                >
                  <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
                          <FileText className="h-4 w-4" />
                        </div>
                        <h3 className="font-semibold text-foreground">{cv.title}</h3>
                        <Badge
                          tone={cv.type === "UploadedPdf" ? "warning" : "default"}
                          className={
                            cv.type === "UploadedPdf"
                              ? "bg-amber-500/10 text-amber-600 hover:bg-amber-500/20"
                              : "bg-blue-500/10 text-blue-600 hover:bg-blue-500/20"
                          }
                        >
                          {cv.type}
                        </Badge>
                        {cv.isDefault ? (
                          <Badge tone="success" className="bg-emerald-500/10 text-emerald-600">
                            Default
                          </Badge>
                        ) : null}
                      </div>
                      {cv.summary ? (
                        <p className="mt-3 text-sm leading-relaxed text-muted-foreground">
                          {cv.summary}
                        </p>
                      ) : null}
                      {cv.fileName ? (
                        <p className="mt-3 text-xs font-medium text-muted-foreground">
                          File: <span className="text-foreground">{cv.fileName}</span>
                        </p>
                      ) : null}
                      <p className="mt-1 text-xs text-muted-foreground/60">
                        Created {new Date(cv.createdAt).toLocaleDateString()}
                      </p>
                    </div>
                    <div className="flex flex-wrap gap-2 md:flex-col lg:flex-row lg:shrink-0">
                      <Button
                        size="sm"
                        variant="outline"
                        className="rounded-xl"
                        onClick={() => setEditingCv(cv)}
                      >
                        Edit
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        className="rounded-xl"
                        disabled={cv.isDefault || setDefaultMutation.isPending}
                        onClick={() => setDefaultMutation.mutate(cv)}
                      >
                        <Star className="h-4 w-4" />
                        Default
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        className="rounded-xl text-destructive hover:bg-destructive/10 hover:text-destructive"
                        disabled={deleteMutation.isPending}
                        onClick={() => deleteMutation.mutate(cv.id)}
                      >
                        <Trash2 className="h-4 w-4" />
                        Delete
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-border p-12 text-center text-muted-foreground">
              <FileText className="h-10 w-10 opacity-20" />
              <p className="mt-4 text-sm font-medium">No CVs yet</p>
              <p className="mt-1 text-xs">
                Create an internal CV or upload a PDF to use later in applications.
              </p>
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
