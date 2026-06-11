import type { LucideIcon } from "lucide-react";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";

interface FeaturePlaceholderProps {
  icon: LucideIcon;
  title: string;
  description: string;
  endpointHint?: string;
}

export function FeaturePlaceholder({ icon: Icon, title, description, endpointHint }: FeaturePlaceholderProps) {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-semibold">{title}</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">{description}</p>
      </div>
      <Panel>
        <PanelHeader>
          <PanelTitle>Phase placeholder</PanelTitle>
        </PanelHeader>
        <PanelBody className="flex gap-4">
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-md bg-muted text-muted-foreground">
            <Icon className="h-5 w-5" />
          </div>
          <div className="space-y-2">
            <p className="text-sm leading-6 text-muted-foreground">
              This page is wired into the authenticated app shell now. Data fetching and forms will be added in its dedicated phase so the UI only renders backend-returned data.
            </p>
            {endpointHint ? <p className="text-xs font-medium text-foreground">{endpointHint}</p> : null}
          </div>
        </PanelBody>
      </Panel>
    </div>
  );
}
