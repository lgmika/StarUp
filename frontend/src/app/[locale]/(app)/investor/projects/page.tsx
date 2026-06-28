"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { Bot, Loader2, Search, Send, Sparkles } from "lucide-react";
import { toast } from "sonner";
import { useForm } from "react-hook-form";
import { RoleGuard } from "@/components/auth/role-guard";
import { ProjectStageBadge, ProjectStatusBadge, ProjectVisibilityBadge } from "@/components/projects/project-badges";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { SystemRoles } from "@/lib/constants";
import { investorInterestSchema, type InvestorInterestFormValues } from "@/lib/validations/investor";
import { investorService } from "@/services";
import type { InvestorProjectDiscoveryDto } from "@/types/investor";

export default function InvestorProjectsPage() {
  return (
    <RoleGuard allowedRoles={[SystemRoles.Investor, SystemRoles.Admin]}>
      <InvestorProjects />
    </RoleGuard>
  );
}

function InvestorProjects() {
  const [items, setItems] = useState<InvestorProjectDiscoveryDto[]>([]);
  const [query, setQuery] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [activeProjectId, setActiveProjectId] = useState<string | null>(null);
  const [summaries, setSummaries] = useState<Record<string, string>>({});
  const form = useForm<InvestorInterestFormValues>({
    resolver: zodResolver(investorInterestSchema),
    defaultValues: { message: "" },
  });

  async function loadProjects(search = "") {
    setIsLoading(true);
    try {
      setItems(await investorService.listProjects(search));
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  }

  async function generateSummary(projectId: string) {
    try {
      const result = await investorService.createInvestorSummary(projectId);
      setSummaries((current) => ({ ...current, [projectId]: result.content }));
      toast.success("Investor summary generated.");
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    }
  }

  async function expressInterest(values: InvestorInterestFormValues) {
    if (!activeProjectId) return;
    try {
      await investorService.expressInterest(activeProjectId, values);
      form.reset({ message: "" });
      setActiveProjectId(null);
      toast.success("Investor interest sent.");
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    }
  }

  useEffect(() => {
    void loadProjects();
  }, []);

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold">Investor Project Discovery</h1>
        <p className="mt-2 text-sm text-muted-foreground">Browse projects visible to investors and express interest.</p>
      </div>
      <form className="flex flex-col gap-2 rounded-lg border border-border bg-card p-3 sm:flex-row" onSubmit={(event) => { event.preventDefault(); void loadProjects(query); }}>
        <Input placeholder="Search investor projects" value={query} onChange={(event) => setQuery(event.target.value)} />
        <Button type="submit" disabled={isLoading}>
          {isLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Search className="h-4 w-4" />}
          Search
        </Button>
      </form>

      {isLoading ? <Panel><PanelBody className="text-sm text-muted-foreground">Loading investor projects...</PanelBody></Panel> : null}
      {!isLoading && items.length === 0 ? <Panel><PanelBody className="py-12 text-center text-sm text-muted-foreground">No investor-visible projects found.</PanelBody></Panel> : null}

      <div className="grid gap-4 xl:grid-cols-2">
        {items.map(({ project, investorSummary }) => (
          <Panel key={project.id}>
            <PanelBody className="space-y-4">
              <div className="flex flex-wrap gap-2">
                <ProjectStatusBadge status={project.status} />
                <ProjectStageBadge stage={project.stage} />
                <ProjectVisibilityBadge visibility={project.visibility} />
                {project.isRecruiting ? <Badge tone="success">Recruiting</Badge> : null}
              </div>
              <div>
                <h2 className="text-base font-semibold">{project.title}</h2>
                <p className="mt-2 text-sm leading-6 text-muted-foreground">{project.summary}</p>
              </div>
              <Panel className="bg-muted/40">
                <PanelBody className="text-sm leading-6 text-muted-foreground">
                  {summaries[project.id] ?? investorSummary ?? "No investor summary returned."}
                </PanelBody>
              </Panel>
              <div className="flex flex-wrap gap-2">
                <Link className="inline-flex h-9 items-center rounded-md border border-border px-3 text-sm font-medium hover:bg-accent" href={`/projects/${project.id}`}>
                  View detail
                </Link>
                <Button variant="outline" onClick={() => void generateSummary(project.id)}>
                  <Bot className="h-4 w-4" />
                  AI summary
                </Button>
                <Button onClick={() => setActiveProjectId(project.id)}>
                  <Sparkles className="h-4 w-4" />
                  Express interest
                </Button>
              </div>
              {activeProjectId === project.id ? (
                <form className="space-y-3 rounded-md border border-border p-3" onSubmit={form.handleSubmit(expressInterest)}>
                  <label className="block space-y-1.5 text-sm font-medium">
                    <span>Message</span>
                    <textarea className="min-h-28 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring" {...form.register("message")} />
                    {form.formState.errors.message ? <span className="text-xs text-destructive">{form.formState.errors.message.message}</span> : null}
                  </label>
                  <div className="flex gap-2">
                    <Button type="submit" disabled={form.formState.isSubmitting}>
                      <Send className="h-4 w-4" />
                      Send interest
                    </Button>
                    <Button type="button" variant="outline" onClick={() => setActiveProjectId(null)}>
                      Cancel
                    </Button>
                  </div>
                </form>
              ) : null}
            </PanelBody>
          </Panel>
        ))}
      </div>
    </div>
  );
}
