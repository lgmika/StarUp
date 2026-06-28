"use client";

import { useQuery } from "@tanstack/react-query";
import { UserRound } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { InlineError } from "@/components/ui/error-boundary";
import { getApiErrorMessage } from "@/lib/api";
import { profileService } from "@/services";

import { ProfileDetailsForm } from "@/components/profile/profile-details-form";
import { ProfilePortfolioForm } from "@/components/profile/profile-portfolio-form";
import { ProfilePreview } from "@/components/profile/profile-preview";
import { ProfileSkillsForm } from "@/components/profile/profile-skills-form";

export default function ProfilePage() {
  const profileQuery = useQuery({
    queryKey: ["profile", "me"],
    queryFn: () => profileService.getMyProfile(),
  });

  if (profileQuery.isLoading) {
    return <LoadingState label="Loading profile" />;
  }

  const profile = profileQuery.data ?? null;

  return (
    <div className="space-y-6">
      {/* Header */}
      <section className="relative overflow-hidden rounded-2xl border border-border/60 bg-gradient-to-r from-primary/[0.04] via-card to-card p-6 shadow-sm">
        <div className="pointer-events-none absolute -right-16 -top-16 h-40 w-40 rounded-full bg-primary/5 blur-3xl" />
        <div className="relative">
          <div className="flex items-center gap-2">
            <UserRound className="h-4 w-4 text-primary" />
            <p className="text-sm font-medium text-muted-foreground">Account</p>
          </div>
          <h1 className="mt-2 text-2xl font-bold tracking-tight">My Profile</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Manage your public founder/member profile and contact visibility from backend profile APIs.
          </p>
        </div>
      </section>

      {profileQuery.error ? (
        <InlineError
          message={getApiErrorMessage(profileQuery.error)}
          onRetry={() => void profileQuery.refetch()}
        />
      ) : null}

      <div className="grid gap-6 xl:grid-cols-[1fr_380px]">
        <section className="space-y-6">
          <ProfileDetailsForm profile={profile} />
          <ProfileSkillsForm profile={profile} />
          <ProfilePortfolioForm profile={profile} />
        </section>

        <aside className="space-y-6">
          <ProfilePreview profile={profile} />
        </aside>
      </div>
    </div>
  );
}
