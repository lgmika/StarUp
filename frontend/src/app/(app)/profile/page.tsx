"use client";

import { useEffect, useMemo, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { Briefcase, Globe, Loader2, MapPin, Plus, Trash2, UserRound } from "lucide-react";
import { toast } from "sonner";
import { useForm } from "react-hook-form";
import { AuthMessage } from "@/components/auth/auth-message";
import { FormField } from "@/components/auth/form-field";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { CONTACT_VISIBILITY_LABELS } from "@/lib/constants";
import {
  addSkillSchema,
  portfolioSchema,
  profileSchema,
  type AddSkillFormValues,
  type PortfolioFormValues,
  type ProfileFormValues,
} from "@/lib/validations/profile";
import { profileService } from "@/services";
import { ContactVisibility } from "@/types/enums";
import type { ProfileDto, SkillDto } from "@/types/user";

const emptyProfileValues: ProfileFormValues = {
  headline: "",
  bio: "",
  location: "",
  phoneNumber: "",
  linkedInUrl: "",
  gitHubUrl: "",
  websiteUrl: "",
  contactVisibility: ContactVisibility.Private,
};

export default function ProfilePage() {
  const [profile, setProfile] = useState<ProfileDto | null>(null);
  const [skills, setSkills] = useState<SkillDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const profileExists = !!profile?.headline || !!profile?.bio;

  const profileForm = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: emptyProfileValues,
  });
  const skillForm = useForm<AddSkillFormValues>({
    resolver: zodResolver(addSkillSchema),
    defaultValues: { skillId: "", yearsOfExperience: "" },
  });
  const portfolioForm = useForm<PortfolioFormValues>({
    resolver: zodResolver(portfolioSchema),
    defaultValues: { title: "", url: "", description: "" },
  });

  const availableSkills = useMemo(() => {
    const selectedIds = new Set(profile?.skills.map((skill) => skill.id) ?? []);
    return skills.filter((skill) => !selectedIds.has(skill.id));
  }, [profile?.skills, skills]);

  async function loadProfile() {
    setIsLoading(true);
    setError(null);
    try {
      const [profileResult, skillResult] = await Promise.all([profileService.getMyProfile(), profileService.listSkills()]);
      setProfile(profileResult);
      setSkills(skillResult);
      profileForm.reset({
        headline: profileResult.headline,
        bio: profileResult.bio,
        location: profileResult.location ?? "",
        phoneNumber: profileResult.phoneNumber ?? "",
        linkedInUrl: profileResult.linkedInUrl ?? "",
        gitHubUrl: profileResult.gitHubUrl ?? "",
        websiteUrl: profileResult.websiteUrl ?? "",
        contactVisibility: profileResult.contactVisibility,
      });
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
    } finally {
      setIsLoading(false);
    }
  }

  async function saveProfile(values: ProfileFormValues) {
    try {
      const request = normalizeProfile(values);
      const nextProfile = profileExists
        ? await profileService.updateProfile(request)
        : await profileService.createProfile(request);
      setProfile(nextProfile);
      toast.success(profileExists ? "Profile updated." : "Profile created.");
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    }
  }

  async function addSkill(values: AddSkillFormValues) {
    try {
      await profileService.addSkill({
        skillId: values.skillId,
        yearsOfExperience: values.yearsOfExperience === "" ? undefined : values.yearsOfExperience,
      });
      skillForm.reset({ skillId: "", yearsOfExperience: "" });
      await loadProfile();
      toast.success("Skill added.");
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    }
  }

  async function removeSkill(skillId: string) {
    try {
      await profileService.removeSkill(skillId);
      await loadProfile();
      toast.success("Skill removed.");
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    }
  }

  async function addPortfolio(values: PortfolioFormValues) {
    try {
      await profileService.createPortfolio({
        title: values.title,
        url: values.url,
        description: cleanOptional(values.description),
      });
      portfolioForm.reset({ title: "", url: "", description: "" });
      await loadProfile();
      toast.success("Portfolio added.");
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    }
  }

  useEffect(() => {
    void loadProfile();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (isLoading) return <LoadingState label="Loading profile" />;

  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_380px]">
      <section className="space-y-6">
        <div>
          <h1 className="text-2xl font-semibold">My Profile</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Manage your public founder/member profile and contact visibility from backend profile APIs.
          </p>
        </div>

        {error ? <AuthMessage tone="error">{error}</AuthMessage> : null}

        <Panel>
          <PanelHeader>
            <PanelTitle>Profile details</PanelTitle>
          </PanelHeader>
          <PanelBody>
            <form className="grid gap-4 md:grid-cols-2" onSubmit={profileForm.handleSubmit(saveProfile)}>
              <div className="md:col-span-2">
                <FormField label="Headline" error={profileForm.formState.errors.headline} registration={profileForm.register("headline")} />
              </div>
              <label className="block space-y-1.5 text-sm font-medium md:col-span-2">
                <span>Bio</span>
                <textarea
                  className="min-h-28 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none transition-colors placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring"
                  {...profileForm.register("bio")}
                />
                {profileForm.formState.errors.bio ? (
                  <span className="block text-xs font-normal text-destructive">{profileForm.formState.errors.bio.message}</span>
                ) : null}
              </label>
              <FormField label="Location" error={profileForm.formState.errors.location} registration={profileForm.register("location")} />
              <FormField label="Phone number" error={profileForm.formState.errors.phoneNumber} registration={profileForm.register("phoneNumber")} />
              <FormField label="LinkedIn URL" error={profileForm.formState.errors.linkedInUrl} registration={profileForm.register("linkedInUrl")} />
              <FormField label="GitHub URL" error={profileForm.formState.errors.gitHubUrl} registration={profileForm.register("gitHubUrl")} />
              <FormField label="Website URL" error={profileForm.formState.errors.websiteUrl} registration={profileForm.register("websiteUrl")} />
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Contact visibility</span>
                <select
                  className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  {...profileForm.register("contactVisibility")}
                >
                  {Object.values(ContactVisibility).map((visibility) => (
                    <option key={visibility} value={visibility}>
                      {CONTACT_VISIBILITY_LABELS[visibility]}
                    </option>
                  ))}
                </select>
              </label>
              <div className="md:col-span-2">
                <Button type="submit" disabled={profileForm.formState.isSubmitting}>
                  {profileForm.formState.isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <UserRound className="h-4 w-4" />}
                  {profileExists ? "Save profile" : "Create profile"}
                </Button>
              </div>
            </form>
          </PanelBody>
        </Panel>

        <Panel>
          <PanelHeader>
            <PanelTitle>Skills</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-4">
            <form className="grid gap-3 md:grid-cols-[1fr_160px_auto]" onSubmit={skillForm.handleSubmit(addSkill)}>
              <label className="block space-y-1.5 text-sm font-medium">
                <span>Skill</span>
                <select className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm" {...skillForm.register("skillId")}>
                  <option value="">Choose skill</option>
                  {availableSkills.map((skill) => (
                    <option key={skill.id} value={skill.id}>
                      {skill.name}
                    </option>
                  ))}
                </select>
              </label>
              <FormField
                label="Years"
                type="number"
                min={0}
                max={60}
                error={skillForm.formState.errors.yearsOfExperience}
                registration={skillForm.register("yearsOfExperience")}
              />
              <div className="flex items-end">
                <Button type="submit" disabled={skillForm.formState.isSubmitting || availableSkills.length === 0}>
                  <Plus className="h-4 w-4" />
                  Add
                </Button>
              </div>
            </form>
            {skillForm.formState.errors.skillId ? (
              <p className="text-xs text-destructive">{skillForm.formState.errors.skillId.message}</p>
            ) : null}
            <div className="flex flex-wrap gap-2">
              {profile?.skills.length ? (
                profile.skills.map((skill) => (
                  <span key={skill.id} className="inline-flex items-center gap-2 rounded-md border border-border px-3 py-1.5 text-sm">
                    {skill.name}
                    {skill.yearsOfExperience != null ? <span className="text-xs text-muted-foreground">{skill.yearsOfExperience}y</span> : null}
                    <button className="text-muted-foreground hover:text-destructive" type="button" onClick={() => void removeSkill(skill.id)}>
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

        <Panel>
          <PanelHeader>
            <PanelTitle>Portfolio</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-4">
            <form className="grid gap-4 md:grid-cols-2" onSubmit={portfolioForm.handleSubmit(addPortfolio)}>
              <FormField label="Title" error={portfolioForm.formState.errors.title} registration={portfolioForm.register("title")} />
              <FormField label="URL" error={portfolioForm.formState.errors.url} registration={portfolioForm.register("url")} />
              <div className="md:col-span-2">
                <FormField label="Description" error={portfolioForm.formState.errors.description} registration={portfolioForm.register("description")} />
              </div>
              <div className="md:col-span-2">
                <Button type="submit" disabled={portfolioForm.formState.isSubmitting}>
                  <Plus className="h-4 w-4" />
                  Add portfolio
                </Button>
              </div>
            </form>
            {profile?.portfolios.length ? (
              <div className="grid gap-3 md:grid-cols-2">
                {profile.portfolios.map((portfolio) => (
                  <a key={portfolio.id} className="rounded-md border border-border p-3 transition-colors hover:bg-accent" href={portfolio.url} rel="noreferrer" target="_blank">
                    <p className="text-sm font-semibold">{portfolio.title}</p>
                    <p className="mt-1 truncate text-xs text-primary">{portfolio.url}</p>
                    {portfolio.description ? <p className="mt-2 text-xs text-muted-foreground">{portfolio.description}</p> : null}
                  </a>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">No portfolio links yet.</p>
            )}
          </PanelBody>
        </Panel>
      </section>

      <aside className="space-y-4">
        <Panel>
          <PanelHeader>
            <PanelTitle>Public preview</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-4">
            <div className="flex h-14 w-14 items-center justify-center rounded-md bg-primary/10 text-primary">
              <UserRound className="h-7 w-7" />
            </div>
            <div>
              <h2 className="text-lg font-semibold">{profile?.fullName}</h2>
              <p className="mt-1 text-sm text-muted-foreground">{profile?.headline || "No headline yet"}</p>
            </div>
            <p className="text-sm leading-6 text-muted-foreground">{profile?.bio || "Create your profile to show a public bio."}</p>
            <div className="space-y-2 text-sm">
              {profile?.location ? <PreviewLine icon={MapPin} text={profile.location} /> : null}
              {profile?.websiteUrl ? <PreviewLine icon={Globe} text={profile.websiteUrl} /> : null}
              <PreviewLine icon={Briefcase} text={CONTACT_VISIBILITY_LABELS[profile?.contactVisibility ?? ContactVisibility.Private]} />
            </div>
            <div className="flex flex-wrap gap-2">
              {profile?.skills.map((skill) => (
                <Badge key={skill.id} tone="muted">
                  {skill.name}
                </Badge>
              ))}
            </div>
          </PanelBody>
        </Panel>
      </aside>
    </div>
  );
}

function PreviewLine({ icon: Icon, text }: { icon: typeof MapPin; text: string }) {
  return (
    <div className="flex items-center gap-2 text-muted-foreground">
      <Icon className="h-4 w-4" />
      <span className="truncate">{text}</span>
    </div>
  );
}

function normalizeProfile(values: ProfileFormValues) {
  return {
    headline: values.headline,
    bio: values.bio,
    location: cleanOptional(values.location),
    phoneNumber: cleanOptional(values.phoneNumber),
    linkedInUrl: cleanOptional(values.linkedInUrl),
    gitHubUrl: cleanOptional(values.gitHubUrl),
    websiteUrl: cleanOptional(values.websiteUrl),
    contactVisibility: values.contactVisibility,
  };
}

function cleanOptional(value?: string) {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}
