import * as React from "react";
import { cn } from "@/lib/utils";

type BadgeTone = "default" | "success" | "warning" | "danger" | "muted";

const toneClasses: Record<BadgeTone, string> = {
  default: "bg-primary/10 text-primary",
  success: "bg-emerald-100 text-emerald-700",
  warning: "bg-amber-100 text-amber-700",
  danger: "bg-red-100 text-red-700",
  muted: "bg-muted text-muted-foreground",
};

export function Badge({
  className,
  tone = "default",
  ...props
}: React.HTMLAttributes<HTMLSpanElement> & { tone?: BadgeTone }) {
  return (
    <span
      className={cn(
        "inline-flex h-6 items-center rounded-md px-2 text-xs font-medium",
        toneClasses[tone],
        className
      )}
      {...props}
    />
  );
}
