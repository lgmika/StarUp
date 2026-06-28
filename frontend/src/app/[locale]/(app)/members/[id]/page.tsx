"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, ExternalLink, Github, Linkedin, MapPin, UserRound } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { Badge } from "@/components/ui/badge";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { profileService } from "@/services/profile-service";

export default function MemberProfilePage() {
  const { id } = useParams<{ id: string }>();
  const query = useQuery({ queryKey: ["public-profile", id], queryFn: () => profileService.getPublicProfile(id) });
  if (query.isLoading) return <LoadingState label="Loading member profile" />;
  if (query.error || !query.data) return <p className="rounded-md bg-destructive/5 p-4 text-sm text-destructive">{getApiErrorMessage(query.error)}</p>;
  const profile = query.data;
  return <div className="space-y-5"><Link href="/search" className="inline-flex items-center gap-2 text-sm font-medium text-primary"><ArrowLeft className="h-4 w-4" />Back to search</Link>
    <Panel><PanelBody className="flex flex-col gap-5 md:flex-row md:items-start"><div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary"><UserRound className="h-8 w-8" /></div><div className="min-w-0 flex-1"><h1 className="text-2xl font-semibold">{profile.fullName}</h1><p className="mt-1 text-base text-muted-foreground">{profile.headline}</p>{profile.location ? <p className="mt-2 flex items-center gap-1 text-sm text-muted-foreground"><MapPin className="h-4 w-4" />{profile.location}</p> : null}<div className="mt-4 flex flex-wrap gap-2">{profile.skills.map((skill) => <Badge key={skill.id} tone="muted">{skill.name}{skill.yearsOfExperience ? ` · ${skill.yearsOfExperience}y` : ""}</Badge>)}</div><div className="mt-4 flex flex-wrap gap-3 text-sm">{profile.linkedInUrl ? <ExternalLinkItem href={profile.linkedInUrl} label="LinkedIn" icon={Linkedin} /> : null}{profile.gitHubUrl ? <ExternalLinkItem href={profile.gitHubUrl} label="GitHub" icon={Github} /> : null}{profile.websiteUrl ? <ExternalLinkItem href={profile.websiteUrl} label="Website" icon={ExternalLink} /> : null}</div></div></PanelBody></Panel>
    <div className="grid gap-4 lg:grid-cols-[1.2fr_1fr]"><Panel><PanelHeader><PanelTitle>About</PanelTitle></PanelHeader><PanelBody><p className="whitespace-pre-wrap text-sm leading-7 text-muted-foreground">{profile.bio || "No biography provided."}</p></PanelBody></Panel><Panel><PanelHeader><PanelTitle>Portfolio</PanelTitle></PanelHeader><PanelBody className="space-y-3">{profile.portfolios.length ? profile.portfolios.map((item) => <a key={item.id} href={item.url} target="_blank" rel="noreferrer" className="block rounded-md border border-border p-3 hover:bg-accent"><p className="font-medium">{item.title}</p>{item.description ? <p className="mt-1 text-sm text-muted-foreground">{item.description}</p> : null}</a>) : <p className="text-sm text-muted-foreground">No portfolio items yet.</p>}</PanelBody></Panel></div>
  </div>;
}

function ExternalLinkItem({ href, label, icon: Icon }: { href: string; label: string; icon: typeof ExternalLink }) { return <a href={href} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-primary hover:underline"><Icon className="h-4 w-4" />{label}</a>; }
