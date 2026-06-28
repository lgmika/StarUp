"use client";

import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, RefreshCw, ScrollText } from "lucide-react";
import { toast } from "sonner";
import { LoadingState } from "@/components/common/loading-state";
import { ProjectWorkspaceNav } from "@/components/projects/project-workspace-nav";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { investorService } from "@/services/investor-service";
import { projectService } from "@/services/project-service";

export default function InvestorSummaryPage() {
  const { id } = useParams<{ id: string }>(); const client = useQueryClient(); const key = ["project-investor-summary", id];
  const query = useQuery({ queryKey: key, queryFn: () => projectService.getInvestorSummary(id), retry: false });
  const generate = useMutation({ mutationFn: () => investorService.createInvestorSummary(id), onSuccess: () => { toast.success("Investor brief generated."); void client.invalidateQueries({ queryKey: key }); }, onError: (error) => toast.error(getApiErrorMessage(error)) });
  if (query.isLoading) return <LoadingState label="Loading investor brief" />;
  return <div className="space-y-5"><PageHeader title="Investor brief" description="AI-generated project summary designed for investment evaluation." actions={<Button onClick={() => generate.mutate()} disabled={generate.isPending}>{generate.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCw className="h-4 w-4" />}{query.data ? "Regenerate" : "Generate brief"}</Button>} /><ProjectWorkspaceNav projectId={id} />
    <Panel><PanelHeader><PanelTitle className="flex items-center gap-2"><ScrollText className="h-4 w-4 text-primary" />Investment narrative</PanelTitle></PanelHeader><PanelBody>{query.data?.content ? <p className="whitespace-pre-wrap text-sm leading-7 text-muted-foreground">{query.data.content}</p> : <div className="py-10 text-center"><p className="font-medium">No investor brief generated</p><p className="mt-2 text-sm text-muted-foreground">Generate a concise investment-oriented project summary.</p>{query.error ? <p className="mt-3 text-xs text-destructive">{getApiErrorMessage(query.error)}</p> : null}</div>}</PanelBody></Panel>
  </div>;
}
