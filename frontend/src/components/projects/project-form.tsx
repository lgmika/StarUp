"use client";

import { useMemo, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, ArrowRight, Save } from "lucide-react";
import { useForm } from "react-hook-form";
import type { UseFormRegisterReturn } from "react-hook-form";
import { FormField } from "@/components/auth/form-field";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { PROJECT_STAGE_LABELS, PROJECT_VISIBILITY_LABELS } from "@/lib/constants";
import { projectFormSchema, type ProjectFormValues } from "@/lib/validations/project";
import { ProjectStage, ProjectVisibility } from "@/types/enums";
import type { ProjectDetailDto, UpsertProjectRequiredRoleDto } from "@/types/project";
import type { SkillDto } from "@/types/user";

const steps = ["Basics", "Details", "Team", "Review"];

export const defaultProjectFormValues: ProjectFormValues = {
  title: "",
  summary: "",
  problem: "",
  solution: "",
  targetMarket: "",
  businessModel: "",
  fundingNeeds: "",
  pitchDeckUrl: "",
  stage: ProjectStage.Idea,
  visibility: ProjectVisibility.Public,
  isRecruiting: true,
  requiredRolesText: "",
  requiredSkillIds: [],
};

interface ProjectFormProps {
  mode: "create" | "edit";
  initialValues?: ProjectFormValues;
  skills: SkillDto[];
  onSubmit: (values: ProjectFormValues) => Promise<void>;
  isSubmitting?: boolean;
}

export function ProjectForm({ mode, initialValues, skills, onSubmit, isSubmitting = false }: ProjectFormProps) {
  const [step, setStep] = useState(0);
  const form = useForm<ProjectFormValues>({
    resolver: zodResolver(projectFormSchema),
    defaultValues: initialValues ?? defaultProjectFormValues,
  });
  const values = form.watch();
  const selectedSkillIds = form.watch("requiredSkillIds");

  const selectedSkills = useMemo(
    () => skills.filter((skill) => selectedSkillIds.includes(skill.id)),
    [selectedSkillIds, skills]
  );

  function toggleSkill(skillId: string) {
    const current = form.getValues("requiredSkillIds");
    form.setValue(
      "requiredSkillIds",
      current.includes(skillId) ? current.filter((id) => id !== skillId) : [...current, skillId],
      { shouldDirty: true }
    );
  }

  async function validateAndNext() {
    const fieldsByStep: Array<Array<keyof ProjectFormValues>> = [
      ["title", "summary", "stage", "visibility"],
      ["problem", "solution", "targetMarket", "businessModel", "fundingNeeds", "pitchDeckUrl"],
      ["isRecruiting", "requiredRolesText", "requiredSkillIds"],
      [],
    ];
    const ok = await form.trigger(fieldsByStep[step]);
    if (ok) setStep((current) => Math.min(current + 1, steps.length - 1));
  }

  return (
    <form className="space-y-5" onSubmit={form.handleSubmit(onSubmit)}>
      <div className="flex flex-wrap gap-2">
        {steps.map((label, index) => (
          <Badge key={label} tone={index === step ? "default" : index < step ? "success" : "muted"}>
            {index + 1}. {label}
          </Badge>
        ))}
      </div>

      <Panel>
        <PanelHeader>
          <PanelTitle>{steps[step]}</PanelTitle>
        </PanelHeader>
        <PanelBody className="space-y-4">
          {step === 0 ? (
            <div className="grid gap-4 md:grid-cols-2">
              <div className="md:col-span-2">
                <FormField label="Project title" error={form.formState.errors.title} registration={form.register("title")} />
              </div>
              <label className="block space-y-1.5 text-sm font-medium md:col-span-2">
                <span>Summary</span>
                <textarea className="min-h-24 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring" {...form.register("summary")} />
                {form.formState.errors.summary ? <span className="text-xs text-destructive">{form.formState.errors.summary.message}</span> : null}
              </label>
              <SelectField label="Stage" value={values.stage} onChange={(value) => form.setValue("stage", value as ProjectStage)}>
                {Object.values(ProjectStage).map((stage) => (
                  <option key={stage} value={stage}>
                    {PROJECT_STAGE_LABELS[stage]}
                  </option>
                ))}
              </SelectField>
              <SelectField label="Visibility" value={values.visibility} onChange={(value) => form.setValue("visibility", value as ProjectVisibility)}>
                {Object.values(ProjectVisibility).map((visibility) => (
                  <option key={visibility} value={visibility}>
                    {PROJECT_VISIBILITY_LABELS[visibility]}
                  </option>
                ))}
              </SelectField>
            </div>
          ) : null}

          {step === 1 ? (
            <div className="grid gap-4 md:grid-cols-2">
              <TextAreaField label="Problem" error={form.formState.errors.problem?.message} registration={form.register("problem")} />
              <TextAreaField label="Solution" error={form.formState.errors.solution?.message} registration={form.register("solution")} />
              <TextAreaField label="Target market" registration={form.register("targetMarket")} />
              <TextAreaField label="Business model" registration={form.register("businessModel")} />
              <TextAreaField label="Funding needs" registration={form.register("fundingNeeds")} />
              <FormField label="Pitch deck URL" error={form.formState.errors.pitchDeckUrl} registration={form.register("pitchDeckUrl")} />
            </div>
          ) : null}

          {step === 2 ? (
            <div className="space-y-4">
              <label className="flex items-center gap-2 text-sm font-medium">
                <input className="h-4 w-4 rounded border-input" type="checkbox" {...form.register("isRecruiting")} />
                Recruiting teammates
              </label>
              <TextAreaField
                label="Required roles"
                hint="One role per line. Use: Role name | Description | Slots"
                registration={form.register("requiredRolesText")}
              />
              <div>
                <p className="text-sm font-medium">Required skills</p>
                <div className="mt-2 flex flex-wrap gap-2">
                  {skills.map((skill) => (
                    <button
                      key={skill.id}
                      className={selectedSkillIds.includes(skill.id) ? "rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-primary-foreground" : "rounded-md bg-muted px-3 py-1.5 text-xs font-medium text-muted-foreground"}
                      type="button"
                      onClick={() => toggleSkill(skill.id)}
                    >
                      {skill.name}
                    </button>
                  ))}
                </div>
              </div>
            </div>
          ) : null}

          {step === 3 ? (
            <div className="grid gap-4 md:grid-cols-2">
              <ReviewLine label="Title" value={values.title || "Missing"} />
              <ReviewLine label="Stage" value={PROJECT_STAGE_LABELS[values.stage]} />
              <ReviewLine label="Visibility" value={PROJECT_VISIBILITY_LABELS[values.visibility]} />
              <ReviewLine label="Recruiting" value={values.isRecruiting ? "Yes" : "No"} />
              <ReviewLine label="Required skills" value={selectedSkills.map((skill) => skill.name).join(", ") || "None"} />
              <ReviewLine label="Required roles" value={values.requiredRolesText || "None"} />
            </div>
          ) : null}
        </PanelBody>
      </Panel>

      <div className="flex flex-wrap justify-between gap-2">
        <Button type="button" variant="outline" disabled={step === 0 || isSubmitting} onClick={() => setStep((current) => Math.max(current - 1, 0))}>
          <ArrowLeft className="h-4 w-4" />
          Back
        </Button>
        {step < steps.length - 1 ? (
          <Button type="button" onClick={() => void validateAndNext()}>
            Next
            <ArrowRight className="h-4 w-4" />
          </Button>
        ) : (
          <Button type="submit" disabled={isSubmitting}>
            <Save className="h-4 w-4" />
            {mode === "create" ? "Create draft" : "Save project"}
          </Button>
        )}
      </div>
    </form>
  );
}

export function projectDetailToFormValues(project: ProjectDetailDto): ProjectFormValues {
  return {
    title: project.title,
    summary: project.summary,
    problem: project.problem,
    solution: project.solution,
    targetMarket: project.targetMarket ?? "",
    businessModel: project.businessModel ?? "",
    fundingNeeds: project.fundingNeeds ?? "",
    pitchDeckUrl: project.pitchDeckUrl ?? "",
    stage: project.stage,
    visibility: project.visibility,
    isRecruiting: project.isRecruiting,
    requiredRolesText: project.requiredRoles
      .map((role) => [role.roleName, role.description ?? "", role.slots].join(" | "))
      .join("\n"),
    requiredSkillIds: project.requiredSkills.map((skill) => skill.id),
  };
}

export function parseRequiredRoles(text?: string): UpsertProjectRequiredRoleDto[] {
  return (text ?? "")
    .split("\n")
    .map((line) => line.trim())
    .filter(Boolean)
    .map((line) => {
      const [roleName, description, slots] = line.split("|").map((part) => part.trim());
      return {
        roleName,
        description: description || undefined,
        slots: Number.isFinite(Number(slots)) && Number(slots) > 0 ? Number(slots) : 1,
        isOpen: true,
      };
    });
}

function SelectField({ label, value, onChange, children }: { label: string; value: string; onChange: (value: string) => void; children: React.ReactNode }) {
  return (
    <label className="block space-y-1.5 text-sm font-medium">
      <span>{label}</span>
      <select className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring" value={value} onChange={(event) => onChange(event.target.value)}>
        {children}
      </select>
    </label>
  );
}

function TextAreaField({ label, hint, error, registration }: { label: string; hint?: string; error?: string; registration: UseFormRegisterReturn }) {
  return (
    <label className="block space-y-1.5 text-sm font-medium">
      <span>{label}</span>
      {hint ? <span className="block text-xs font-normal text-muted-foreground">{hint}</span> : null}
      <textarea className="min-h-28 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring" {...registration} />
      {error ? <span className="text-xs text-destructive">{error}</span> : null}
    </label>
  );
}

function ReviewLine({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-md border border-border p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 whitespace-pre-wrap text-sm font-medium">{value}</p>
    </div>
  );
}
