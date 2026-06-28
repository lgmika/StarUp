"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { ProjectForm, parseRequiredRoles } from "@/components/projects/project-form";
import { AuthMessage } from "@/components/auth/auth-message";
import { LoadingState } from "@/components/common/loading-state";
import { getApiErrorMessage } from "@/lib/api";
import type { ProjectFormValues } from "@/lib/validations/project";
import { profileService, projectService } from "@/services";
import type { SkillDto } from "@/types/user";

export default function CreateProjectPage() {
  const router = useRouter();
  const [skills, setSkills] = useState<SkillDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadSkills() {
      try {
        setSkills(await profileService.listSkills());
      } catch (loadError) {
        setError(getApiErrorMessage(loadError));
      } finally {
        setIsLoading(false);
      }
    }

    void loadSkills();
  }, []);

  async function createDraft(values: ProjectFormValues) {
    setIsSubmitting(true);
    try {
      const project = await projectService.createDraft({
        title: values.title,
        summary: values.summary,
        problem: values.problem,
        solution: values.solution,
        stage: values.stage,
        visibility: values.visibility,
      });

      await projectService.updateProject(project.id, {
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

      toast.success("Project draft created.");
      router.push(`/projects/${project.id}/edit`);
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    } finally {
      setIsSubmitting(false);
    }
  }

  if (isLoading) return <LoadingState label="Loading project form" />;

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold">Create Project</h1>
        <p className="mt-2 text-sm text-muted-foreground">Create a draft first, then enrich it with team needs and optional details.</p>
      </div>
      {error ? <AuthMessage tone="error">{error}</AuthMessage> : null}
      <ProjectForm mode="create" skills={skills} isSubmitting={isSubmitting} onSubmit={createDraft} />
    </div>
  );
}

function cleanOptional(value?: string) {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}
