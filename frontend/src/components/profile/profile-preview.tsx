"use client";

import { Briefcase, Globe, MapPin, UserRound } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { CONTACT_VISIBILITY_LABELS } from "@/lib/constants";
import { ContactVisibility } from "@/types/enums";
import type { ProfileDto } from "@/types/user";

export function ProfilePreview({ profile }: { profile: ProfileDto | null }) {
  return (
    <Panel>
      <PanelHeader>
        <PanelTitle>Public preview</PanelTitle>
      </PanelHeader>
      <PanelBody className="space-y-6">
        <div className="flex h-16 w-16 items-center justify-center rounded-xl bg-primary/10 text-primary">
          <UserRound className="h-8 w-8" />
        </div>
        
        <div>
          <h2 className="text-xl font-bold">{profile?.fullName || "Your Name"}</h2>
          <p className="mt-1 font-medium text-muted-foreground">
            {profile?.headline || "No headline yet"}
          </p>
        </div>
        
        <p className="text-sm leading-relaxed text-muted-foreground">
          {profile?.bio || "Create your profile to show a public bio. This will be visible to other members when you interact or apply to projects."}
        </p>
        
        <div className="space-y-3 text-sm">
          {profile?.location ? <PreviewLine icon={MapPin} text={profile.location} /> : null}
          {profile?.websiteUrl ? <PreviewLine icon={Globe} text={profile.websiteUrl} /> : null}
          <PreviewLine
            icon={Briefcase}
            text={CONTACT_VISIBILITY_LABELS[profile?.contactVisibility ?? ContactVisibility.Private]}
          />
        </div>
        
        <div className="flex flex-wrap gap-2 pt-2">
          {profile?.skills.map((skill) => (
            <Badge key={skill.id} tone="muted" className="bg-muted/50 text-muted-foreground hover:bg-muted">
              {skill.name}
            </Badge>
          ))}
          {(!profile?.skills || profile.skills.length === 0) && (
            <span className="text-xs italic text-muted-foreground">No skills to display</span>
          )}
        </div>
      </PanelBody>
    </Panel>
  );
}

function PreviewLine({ icon: Icon, text }: { icon: typeof MapPin; text: string }) {
  return (
    <div className="flex items-center gap-3 text-muted-foreground">
      <Icon className="h-4 w-4 shrink-0 opacity-70" />
      <span className="truncate">{text}</span>
    </div>
  );
}
