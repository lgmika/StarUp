"use client";

import { useEffect } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, Plus } from "lucide-react";
import { toast } from "sonner";
import { useForm } from "react-hook-form";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { FormField } from "@/components/auth/form-field";
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

interface CvFormProps {
  editingCv: CvDto | null;
  onCancelEdit: () => void;
}

export function CvForm({ editingCv, onCancelEdit }: CvFormProps) {
  const queryClient = useQueryClient();

  const form = useForm<CvFormValues>({
    resolver: zodResolver(cvSchema),
    defaultValues: defaultCvValues,
  });

  useEffect(() => {
    if (editingCv) {
      form.reset({
        title: editingCv.title,
        summary: editingCv.summary ?? "",
        experienceJson: editingCv.experienceJson ?? "",
        educationJson: editingCv.educationJson ?? "",
        isDefault: editingCv.isDefault,
      });
    } else {
      form.reset(defaultCvValues);
    }
  }, [editingCv, form]);

  const mutation = useMutation({
    mutationFn: async (values: CvFormValues) => {
      const request = {
        title: values.title,
        summary: cleanOptional(values.summary),
        experienceJson: cleanOptional(values.experienceJson),
        educationJson: cleanOptional(values.educationJson),
        isDefault: values.isDefault,
      };

      if (editingCv) {
        return profileService.updateCv(editingCv.id, request);
      } else {
        return profileService.createCv(request);
      }
    },
    onSuccess: () => {
      toast.success(editingCv ? "CV updated." : "CV created.");
      form.reset(defaultCvValues);
      onCancelEdit();
      void queryClient.invalidateQueries({ queryKey: ["cvs"] });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  return (
    <Panel>
      <PanelHeader>
        <PanelTitle>{editingCv ? "Edit CV" : "Create CV"}</PanelTitle>
      </PanelHeader>
      <PanelBody>
        <form className="space-y-4" onSubmit={form.handleSubmit((v) => mutation.mutate(v))}>
          <FormField
            label="Title"
            error={form.formState.errors.title}
            registration={form.register("title")}
          />
          <label className="block space-y-1.5 text-sm font-medium">
            <span>Summary</span>
            <textarea
              className="min-h-24 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
              {...form.register("summary")}
            />
          </label>
          <label className="block space-y-1.5 text-sm font-medium">
            <span>Experience JSON</span>
            <textarea
              className="min-h-20 w-full rounded-md border border-input bg-background px-3 py-2 font-mono text-xs outline-none focus-visible:ring-2 focus-visible:ring-ring"
              {...form.register("experienceJson")}
            />
          </label>
          <label className="block space-y-1.5 text-sm font-medium">
            <span>Education JSON</span>
            <textarea
              className="min-h-20 w-full rounded-md border border-input bg-background px-3 py-2 font-mono text-xs outline-none focus-visible:ring-2 focus-visible:ring-ring"
              {...form.register("educationJson")}
            />
          </label>
          <label className="flex items-center gap-2 text-sm font-medium">
            <input
              className="h-4 w-4 rounded border-input"
              type="checkbox"
              {...form.register("isDefault")}
            />
            Set as default CV
          </label>
          <div className="flex flex-wrap gap-2 pt-2">
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
              {editingCv ? "Save CV" : "Create CV"}
            </Button>
            {editingCv ? (
              <Button type="button" variant="outline" onClick={onCancelEdit}>
                Cancel
              </Button>
            ) : null}
          </div>
        </form>
      </PanelBody>
    </Panel>
  );
}

function cleanOptional(value?: string) {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}
