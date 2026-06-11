import { AlertCircle, CheckCircle2 } from "lucide-react";
import { cn } from "@/lib/utils";

interface AuthMessageProps {
  tone: "error" | "success";
  children: React.ReactNode;
}

export function AuthMessage({ tone, children }: AuthMessageProps) {
  const Icon = tone === "error" ? AlertCircle : CheckCircle2;

  return (
    <div
      className={cn(
        "flex gap-2 rounded-md border px-3 py-2 text-sm",
        tone === "error"
          ? "border-destructive/30 bg-destructive/10 text-destructive"
          : "border-emerald-200 bg-emerald-50 text-emerald-700"
      )}
    >
      <Icon className="mt-0.5 h-4 w-4 shrink-0" />
      <div>{children}</div>
    </div>
  );
}
