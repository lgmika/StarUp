"use client";

import { useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, UserRound } from "lucide-react";
import { toast } from "sonner";
import { useForm } from "react-hook-form";
import { RoleGuard } from "@/components/auth/role-guard";
import { FormField } from "@/components/auth/form-field";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { investorProfileSchema, type InvestorProfileFormValues } from "@/lib/validations/investor";
import { investorService } from "@/services";
import type { InvestorProfileDto } from "@/types/investor";

const emptyValues: InvestorProfileFormValues = {
  displayName: "",
  organizationName: "",
  bio: "",
  investmentFocus: "",
  websiteUrl: "",
  linkedInUrl: "",
  minTicketSize: "",
  maxTicketSize: "",
};

export default function InvestorProfilePage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Investor, SystemRoles.Admin]}>
      <InvestorProfileForm />
    </RoleGuard>
  );
}

function InvestorProfileForm() {
  const [profile, setProfile] = useState<InvestorProfileDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const form = useForm<InvestorProfileFormValues>({
    resolver: zodResolver(investorProfileSchema),
    defaultValues: emptyValues,
  });

  useEffect(() => {
    async function loadProfile() {
      try {
        const result = await investorService.getMyProfile();
        setProfile(result);
        form.reset({
          displayName: result.displayName,
          organizationName: result.organizationName ?? "",
          bio: result.bio ?? "",
          investmentFocus: result.investmentFocus ?? "",
          websiteUrl: result.websiteUrl ?? "",
          linkedInUrl: result.linkedInUrl ?? "",
          minTicketSize: result.minTicketSize ?? "",
          maxTicketSize: result.maxTicketSize ?? "",
        });
      } catch {
        setProfile(null);
      } finally {
        setIsLoading(false);
      }
    }

    void loadProfile();
  }, [form]);

  async function saveProfile(values: InvestorProfileFormValues) {
    try {
      const request = {
        displayName: values.displayName,
        organizationName: cleanOptional(values.organizationName),
        bio: cleanOptional(values.bio),
        investmentFocus: cleanOptional(values.investmentFocus),
        websiteUrl: cleanOptional(values.websiteUrl),
        linkedInUrl: cleanOptional(values.linkedInUrl),
        minTicketSize: values.minTicketSize === "" ? undefined : Number(values.minTicketSize),
        maxTicketSize: values.maxTicketSize === "" ? undefined : Number(values.maxTicketSize),
      };
      const nextProfile = profile
        ? await investorService.updateProfile(request)
        : await investorService.createProfile(request);
      setProfile(nextProfile);
      toast.success(profile ? "Investor profile updated." : "Investor profile created.");
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    }
  }

  if (isLoading) return <LoadingState label="Loading investor profile" />;

  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_360px]">
      <section className="space-y-5">
        <div>
          <h1 className="text-2xl font-semibold">Investor Profile</h1>
          <p className="mt-2 text-sm text-muted-foreground">Create or update your investor profile for investor discovery and interest flows.</p>
        </div>
        <Panel>
          <PanelHeader>
            <PanelTitle>{profile ? "Update profile" : "Create profile"}</PanelTitle>
          </PanelHeader>
          <PanelBody>
            <form className="grid gap-4 md:grid-cols-2" onSubmit={form.handleSubmit(saveProfile)}>
              <FormField label="Display name" error={form.formState.errors.displayName} registration={form.register("displayName")} />
              <FormField label="Organization" error={form.formState.errors.organizationName} registration={form.register("organizationName")} />
              <FormField label="Website URL" error={form.formState.errors.websiteUrl} registration={form.register("websiteUrl")} />
              <FormField label="LinkedIn URL" error={form.formState.errors.linkedInUrl} registration={form.register("linkedInUrl")} />
              <FormField label="Min ticket size" type="number" error={form.formState.errors.minTicketSize} registration={form.register("minTicketSize")} />
              <FormField label="Max ticket size" type="number" error={form.formState.errors.maxTicketSize} registration={form.register("maxTicketSize")} />
              <TextArea label="Bio" error={form.formState.errors.bio?.message} registration={form.register("bio")} />
              <TextArea label="Investment focus" error={form.formState.errors.investmentFocus?.message} registration={form.register("investmentFocus")} />
              <div className="md:col-span-2">
                <Button type="submit" disabled={form.formState.isSubmitting}>
                  {form.formState.isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <UserRound className="h-4 w-4" />}
                  {profile ? "Save profile" : "Create profile"}
                </Button>
              </div>
            </form>
          </PanelBody>
        </Panel>
      </section>
      <aside>
        <Panel>
          <PanelHeader>
            <PanelTitle>Preview</PanelTitle>
          </PanelHeader>
          <PanelBody className="space-y-3">
            <p className="text-lg font-semibold">{(profile?.displayName ?? form.watch("displayName")) || "Investor name"}</p>
            <p className="text-sm text-muted-foreground">{(profile?.organizationName ?? form.watch("organizationName")) || "Organization"}</p>
            <p className="text-sm leading-6 text-muted-foreground">{(profile?.investmentFocus ?? form.watch("investmentFocus")) || "Investment focus appears here."}</p>
          </PanelBody>
        </Panel>
      </aside>
    </div>
  );
}

function TextArea({ label, error, registration }: { label: string; error?: string; registration: ReturnType<typeof useForm<InvestorProfileFormValues>>["register"] extends (name: infer _Name) => infer Result ? Result : never }) {
  return (
    <label className="block space-y-1.5 text-sm font-medium md:col-span-2">
      <span>{label}</span>
      <textarea className="min-h-28 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring" {...registration} />
      {error ? <span className="text-xs text-destructive">{error}</span> : null}
    </label>
  );
}

function cleanOptional(value?: string) {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}
