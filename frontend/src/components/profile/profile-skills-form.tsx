"use client";

import { useMemo } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { useForm } from "react-hook-form";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FormField } from "@/components/auth/form-field";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { addSkillSchema, type AddSkillFormValues } from "@/lib/validations/profile";
import { profileService } from "@/services";
import type { ProfileDto } from "@/types/user";

export function ProfileSkillsForm({ profile }: { profile: ProfileDto | null }) {
  const queryClient = useQueryClient();

  const skillsQuery = useQuery({
    queryKey: ["skills", "list"],
    queryFn: () => profileService.listSkills(),
  });

  const form = useForm<AddSkillFormValues>({
    resolver: zodResolver(addSkillSchema),
    defaultValues: { skillId: "", yearsOfExperience: "" },
  });

  const availableSkills = useMemo(() => {
    const allSkills = skillsQuery.data ?? [];
    const selectedIds = new Set(profile?.skills.map((skill) => skill.id) ?? []);
    return allSkills.filter((skill) => !selectedIds.has(skill.id));
  }, [profile?.skills, skillsQuery.data]);

  const addMutation = useMutation({
    mutationFn: async (values: AddSkillFormValues) => {
      return profileService.addSkill({
        skillId: values.skillId,
        yearsOfExperience: values.yearsOfExperience === "" ? undefined : values.yearsOfExperience,
      });
    },
    onSuccess: () => {
      toast.success("Skill added.");
      form.reset({ skillId: "", yearsOfExperience: "" });
      void queryClient.invalidateQueries({ queryKey: ["profile", "me"] });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  const removeMutation = useMutation({
    mutationFn: (skillId: string) => profileService.removeSkill(skillId),
    onSuccess: () => {
      toast.success("Skill removed.");
      void queryClient.invalidateQueries({ queryKey: ["profile", "me"] });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  return (
    <Panel>
      <PanelHeader>
        <PanelTitle>Skills</PanelTitle>
      </PanelHeader>
      <PanelBody className="space-y-4">
        <form
          className="grid gap-3 md:grid-cols-[1fr_160px_auto]"
          onSubmit={form.handleSubmit((v) => addMutation.mutate(v))}
        >
          <label className="block space-y-1.5 text-sm font-medium">
            <span>Skill</span>
            <select
              className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
              {...form.register("skillId")}
              disabled={skillsQuery.isLoading}
            >
              <option value="">{skillsQuery.isLoading ? "Loading skills..." : "Choose skill"}</option>
              {availableSkills.map((skill) => (
                <option key={skill.id} value={skill.id}>
                  {skill.name}
                </option>
              ))}
            </select>
            {form.formState.errors.skillId ? (
              <span className="text-xs text-destructive">
                {form.formState.errors.skillId.message}
              </span>
            ) : null}
          </label>
          <FormField
            label="Years"
            type="number"
            min={0}
            max={60}
            error={form.formState.errors.yearsOfExperience}
            registration={form.register("yearsOfExperience")}
          />
          <div className="flex items-end pb-1.5">
            <Button
              type="submit"
              disabled={
                addMutation.isPending || availableSkills.length === 0 || skillsQuery.isLoading
              }
            >
              {addMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
              Add
            </Button>
          </div>
        </form>

        <div className="flex flex-wrap gap-2 pt-2">
          {profile?.skills.length ? (
            profile.skills.map((skill) => (
              <span
                key={skill.id}
                className="inline-flex items-center gap-2 rounded-md border border-border px-3 py-1.5 text-sm transition-colors hover:bg-muted/50"
              >
                {skill.name}
                {skill.yearsOfExperience != null ? (
                  <span className="text-xs text-muted-foreground">
                    {skill.yearsOfExperience}y
                  </span>
                ) : null}
                <button
                  className="text-muted-foreground hover:text-destructive"
                  type="button"
                  onClick={() => removeMutation.mutate(skill.id)}
                  disabled={removeMutation.isPending}
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              </span>
            ))
          ) : (
            <p className="text-sm text-muted-foreground">No skills added yet.</p>
          )}
        </div>
      </PanelBody>
    </Panel>
  );
}
