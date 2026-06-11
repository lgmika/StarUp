import { TriangleAlert } from "lucide-react";
import { Badge } from "@/components/ui/badge";

export function RiskFlags({ flags }: { flags: string[] }) {
  if (!flags.length) return <span className="text-sm text-muted-foreground">No AI risk flags returned.</span>;

  return (
    <div className="flex flex-wrap gap-2">
      {flags.map((flag) => (
        <Badge key={flag} tone="warning">
          <TriangleAlert className="mr-1 h-3 w-3" />
          {flag}
        </Badge>
      ))}
    </div>
  );
}
