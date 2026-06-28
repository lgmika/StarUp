"use client";

import { useEffect } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, UserRound } from "lucide-react";
import { toast } from "sonner";
import { useForm } from "react-hook-form";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { FormField } from "@/components/auth/form-field";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { CONTACT_VISIBILITY_LABELS } from "@/lib/constants";
import { profileSchema, type ProfileFormValues } from "@/lib/validations/profile";
import { profileService } from "@/services";
import { ContactVisibility } from "@/types/enums";
import type { ProfileDto } from "@/types/user";

export function ProfileDetailsForm({ profile }: { profile: ProfileDto | null }) {
  const queryClient = useQueryClient();
  const profileExists = !!profile?.headline || !!profile?.bio;

  const form = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      headline: profile?.headline ?? "",
      bio: profile?.bio ?? "",
      location: profile?.location ?? "",
      phoneNumber: profile?.phoneNumber ?? "",
      linkedInUrl: profile?.linkedInUrl ?? "",
      gitHubUrl: profile?.gitHubUrl ?? "",
      websiteUrl: profile?.websiteUrl ?? "",
      contactVisibility: profile?.contactVisibility ?? ContactVisibility.Private,
    },
  });

  // Update form if profile is refreshed from backend
  useEffect(() => {
    if (profile) {
      form.reset({
        headline: profile.headline ?? "",
        bio: profile.bio ?? "",
        location: profile.location ?? "",
        phoneNumber: profile.phoneNumber ?? "",
        linkedInUrl: profile.linkedInUrl ?? "",
        gitHubUrl: profile.gitHubUrl ?? "",
        websiteUrl: profile.websiteUrl ?? "",
        contactVisibility: profile.contactVisibility ?? ContactVisibility.Private,
      });
    }
  }, [profile, form]);

  const mutation = useMutation({
    mutationFn: async (values: ProfileFormValues) => {
      const request = {
        headline: values.headline,
        bio: values.bio,
        location: cleanOptional(values.location),
        phoneNumber: cleanOptional(values.phoneNumber),
        linkedInUrl: cleanOptional(values.linkedInUrl),
        gitHubUrl: cleanOptional(values.gitHubUrl),
        websiteUrl: cleanOptional(values.websiteUrl),
        contactVisibility: values.contactVisibility,
      };
      if (profileExists) {
        return profileService.updateProfile(request);
      } else {
        return profileService.createProfile(request);
      }
    },
    onSuccess: () => {
      toast.success(profileExists ? "Profile updated." : "Profile created.");
      void queryClient.invalidateQueries({ queryKey: ["profile", "me"] });
    },
    onError: (error) => {
      toast.error(getApiErrorMessage(error));
    },
  });

  return (
    <Panel>
      <PanelHeader>
        <PanelTitle>Profile details</PanelTitle>
      </PanelHeader>
      <PanelBody>
        <form
          className="grid gap-4 md:grid-cols-2"
          onSubmit={form.handleSubmit((v) => mutation.mutate(v))}
        >
          <div className="md:col-span-2">
            <FormField
              label="Headline"
              error={form.formState.errors.headline}
              registration={form.register("headline")}
            />
          </div>
          <label className="block space-y-1.5 text-sm font-medium md:col-span-2">
            <span>Bio</span>
            <textarea
              className="min-h-28 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none transition-colors placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring"
              {...form.register("bio")}
            />
            {form.formState.errors.bio ? (
              <span className="block text-xs font-normal text-destructive">
                {form.formState.errors.bio.message}
              </span>
            ) : null}
          </label>
          <FormField
            label="Location"
            error={form.formState.errors.location}
            registration={form.register("location")}
          />
          <FormField
            label="Phone number"
            error={form.formState.errors.phoneNumber}
            registration={form.register("phoneNumber")}
          />
          <FormField
            label="LinkedIn URL"
            error={form.formState.errors.linkedInUrl}
            registration={form.register("linkedInUrl")}
          />
          <FormField
            label="GitHub URL"
            error={form.formState.errors.gitHubUrl}
            registration={form.register("gitHubUrl")}
          />
          <FormField
            label="Website URL"
            error={form.formState.errors.websiteUrl}
            registration={form.register("websiteUrl")}
          />
          <label className="block space-y-1.5 text-sm font-medium">
            <span>Contact visibility</span>
            <select
              className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
              {...form.register("contactVisibility")}
            >
              {Object.values(ContactVisibility).map((visibility) => (
                <option key={visibility} value={visibility}>
                  {CONTACT_VISIBILITY_LABELS[visibility]}
                </option>
              ))}
            </select>
          </label>
          <div className="md:col-span-2">
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <UserRound className="h-4 w-4" />
              )}
              {profileExists ? "Save profile" : "Create profile"}
            </Button>
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
