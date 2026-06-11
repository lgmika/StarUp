import { Loader2 } from "lucide-react";

export function LoadingState({ label = "Loading" }: { label?: string }) {
  return (
    <div className="flex min-h-[240px] items-center justify-center rounded-lg border border-border bg-card p-6 text-sm text-muted-foreground">
      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
      {label}
    </div>
  );
}
