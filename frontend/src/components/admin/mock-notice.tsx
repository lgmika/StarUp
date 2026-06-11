import { DatabaseZap } from "lucide-react";

export function MockNotice({ label }: { label: string }) {
  return (
    <div className="flex items-start gap-2 rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
      <DatabaseZap className="mt-0.5 h-4 w-4 shrink-0" />
      <p>{label} is using frontend mock service because a complete backend endpoint is not available yet.</p>
    </div>
  );
}
