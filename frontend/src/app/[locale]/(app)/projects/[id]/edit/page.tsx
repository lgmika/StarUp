"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { toast } from "sonner";
import { AiProjectPanel } from "@/components/projects/ai-project-panel";
import { ProjectForm, parseRequiredRoles, projectDetailToFormValues } from "@/components/projects/project-form";
import { AuthMessage } from "@/components/auth/auth-message";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { getApiErrorMessage } from "@/lib/api";
import type { ProjectFormValues } from "@/lib/validations/project";
import { profileService, projectService } from "@/services";
import type { ProjectDetailDto } from "@/types/project";
import type { SkillDto } from "@/types/user";

export default function EditProjectPage() {
  const params = useParams<{ id: string }>();
  const [project, setProject] = useState<ProjectDetailDto | null>(null);
  const [skills, setSkills] = useState<SkillDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function loadPage() {
    setIsLoading(true);
    setError(null);
    try {
      const [projectResult, skillResult] = await Promise.all([
        projectService.getProject(params.id),
        profileService.listSkills(),
      ]);
      setProject(projectResult);
      setSkills(skillResult);
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
    } finally {
      setIsLoading(false);
    }
  }

  async function updateProject(values: ProjectFormValues) {
    if (!project) return;
    setIsSubmitting(true);
    try {
      const nextProject = await projectService.updateProject(project.id, {
        title: values.title,
        summary: values.summary,
        problem: values.problem,
        solution: values.solution,
        targetMarket: cleanOptional(values.targetMarket),
        businessModel: cleanOptional(values.businessModel),
        fundingNeeds: cleanOptional(values.fundingNeeds),
        pitchDeckUrl: cleanOptional(values.pitchDeckUrl),
        stage: values.stage,
        visibility: values.visibility,
        isRecruiting: values.isRecruiting,
        requiredRoles: parseRequiredRoles(values.requiredRolesText),
        requiredSkillIds: values.requiredSkillIds,
      });
      setProject(nextProject);
      toast.success("Project updated.");
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    } finally {
      setIsSubmitting(false);
    }
  }

  async function submitForReview() {
    if (!project) return;
    try {
      await projectService.submitReview(project.id);
      await loadPage();
      toast.success("Project submitted for review.");
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    }
  }

  useEffect(() => {
    void loadPage();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params.id]);

  if (isLoading) return <LoadingState label="Loading project editor" />;

  if (!project) {
    return (
      <div className="space-y-4">
        {error ? <AuthMessage tone="error">{error}</AuthMessage> : null}
        <Link className="text-sm font-medium text-primary hover:underline" href="/projects">
          Back to projects
        </Link>
      </div>
    );
  }

  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_380px]">
      <section className="space-y-5">
        <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
          <div>
            <h1 className="text-2xl font-semibold">Edit Project</h1>
            <p className="mt-2 text-sm text-muted-foreground">Update project details, team needs, and AI readiness before review.</p>
          </div>
          <div className="flex gap-2">
            <Link className="inline-flex h-10 items-center justify-center rounded-md border border-border bg-background px-4 text-sm font-medium hover:bg-accent" href={`/projects/${project.id}`}>
              View detail
            </Link>
            <Button type="button" variant="outline" onClick={() => void submitForReview()}>
              Submit review
            </Button>
          </div>
        </div>
        {error ? <AuthMessage tone="error">{error}</AuthMessage> : null}
        <ProjectForm
          mode="edit"
          initialValues={projectDetailToFormValues(project)}
          skills={skills}
          isSubmitting={isSubmitting}
          onSubmit={updateProject}
        />
      </section>
      <aside>
        <AiProjectPanel projectId={project.id} />
      </aside>
    </div>
  );
}

function cleanOptional(value?: string) {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}
