"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { FileText, Loader2, Send, ShieldCheck } from "lucide-react";
import { toast } from "sonner";
import { useForm } from "react-hook-form";
import { AuthMessage } from "@/components/auth/auth-message";
import { LoadingState } from "@/components/common/loading-state";
import { ProjectStageBadge, ProjectStatusBadge, ProjectVisibilityBadge } from "@/components/projects/project-badges";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { applyProjectSchema, type ApplyProjectFormValues } from "@/lib/validations/application";
import { applicationService, profileService, projectService } from "@/services";
import { useAuthStore } from "@/stores/auth-store";
import { ProjectStatus } from "@/types/enums";
import type { ProjectDetailDto } from "@/types/project";
import type { CvDto } from "@/types/user";

export default function ApplyProjectPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const user = useAuthStore((state) => state.user);
  const [project, setProject] = useState<ProjectDetailDto | null>(null);
  const [cvs, setCvs] = useState<CvDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const form = useForm<ApplyProjectFormValues>({
    resolver: zodResolver(applyProjectSchema),
    defaultValues: { cvId: "", coverLetter: "" },
  });

  const isVerified = useMemo(() => user?.roles.includes(SystemRoles.VerifiedUser) || user?.roles.includes(SystemRoles.Admin), [user?.roles]);
  const canApply = project?.status === ProjectStatus.Published && project.isRecruiting && isVerified;

  useEffect(() => {
    async function loadPage() {
      setIsLoading(true);
      setError(null);
      try {
        const [projectResult, cvResult] = await Promise.all([
          projectService.getProject(params.id),
          profileService.listCvs(),
        ]);
        setProject(projectResult);
        setCvs(cvResult);
        const defaultCv = cvResult.find((cv) => cv.isDefault) ?? cvResult[0];
        if (defaultCv) form.setValue("cvId", defaultCv.id);
      } catch (loadError) {
        setError(getApiErrorMessage(loadError));
      } finally {
        setIsLoading(false);
      }
    }

    void loadPage();
  }, [form, params.id]);

  async function submitApplication(values: ApplyProjectFormValues) {
    if (!project) return;
    try {
      const application = await applicationService.apply(project.id, {
        cvId: values.cvId || undefined,
        coverLetter: values.coverLetter,
      });
      toast.success("Application submitted.");
      router.push(`/applications/${application.id}?projectId=${application.projectId}`);
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    }
  }

  if (isLoading) return <LoadingState label="Loading application form" />;

  if (error || !project) {
    return (
      <div className="space-y-4">
        <AuthMessage tone="error">{error ?? "Project could not be loaded."}</AuthMessage>
        <Link className="text-sm font-medium text-primary hover:underline" href="/projects">
          Back to projects
        </Link>
      </div>
    );
  }

  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_380px]">
      <section className="space-y-5">
        <div>
          <h1 className="text-2xl font-semibold">Apply to {project.title}</h1>
          <p className="mt-2 text-sm text-muted-foreground">Submit a cover letter and optional CV. Backend requires a verified user.</p>
        </div>

        {!isVerified ? (
          <AuthMessage tone="error">
            Your account must be email verified before applying. Use a VerifiedUser account or complete email verification.
          </AuthMessage>
        ) : null}
        {project.status !== ProjectStatus.Published || !project.isRecruiting ? (
          <AuthMessage tone="error">This project is not currently accepting applications.</AuthMessage>
        ) : null}

        <Panel>
          <PanelHeader>
            <PanelTitle>Application</PanelTitle>
          </PanelHeader>
          <PanelBody>
            <form className="space-y-4" onSubmit={form.handleSubmit(submitApplication)}>
              <label className="block space-y-1.5 text-sm font-medium">
                <span>CV</span>
                <select className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring" {...form.register("cvId")}>
                  <option value="">No CV selected</option>
                  {cvs.map((cv) => (
                    <option key={cv.id} value={cv.id}>
                      {cv.title}{cv.isDefault ? " (default)" : ""}
                    </option>
                  ))}
                </select>
              </label>
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Cover letter</span>
                <textarea className="min-h-48 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring" {...form.register("coverLetter")} />
                {form.formState.errors.coverLetter ? <span className="text-xs text-destructive">{form.formState.errors.coverLetter.message}</span> : null}
              </label>
              <Button type="submit" disabled={!canApply || form.formState.isSubmitting}>
                {form.formState.isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
                Submit application
              </Button>
            </form>
          </PanelBody>
        </Panel>
      </section>

      <aside className="space-y-4">
        <Panel>
          <PanelHeader>
            <PanelTitle>Project snapshot</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-3">
            <div className="flex flex-wrap gap-2">
              <ProjectStatusBadge status={project.status} />
              <ProjectStageBadge stage={project.stage} />
              <ProjectVisibilityBadge visibility={project.visibility} />
              {project.isRecruiting ? <Badge tone="success">Recruiting</Badge> : <Badge tone="muted">Not recruiting</Badge>}
            </div>
            <p className="text-sm leading-6 text-muted-foreground">{project.summary}</p>
          </PanelBody>
        </Panel>
        <Panel>
          <PanelHeader>
            <PanelTitle>CV selection</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-3 text-sm text-muted-foreground">
            {cvs.length ? (
              cvs.map((cv) => (
                <div key={cv.id} className="flex items-center gap-2 rounded-md border border-border p-2">
                  <FileText className="h-4 w-4" />
                  <span>{cv.title}</span>
                  {cv.isDefault ? <Badge tone="success">Default</Badge> : null}
                </div>
              ))
            ) : (
              <p>No CVs yet. You can still apply without a CV, or create one from the CV page.</p>
            )}
            <Link className="inline-flex items-center gap-2 text-sm font-medium text-primary hover:underline" href="/cvs">
              <ShieldCheck className="h-4 w-4" />
              Manage CVs
            </Link>
          </PanelBody>
        </Panel>
      </aside>
    </div>
  );
}
