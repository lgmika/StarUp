import { CheckCircle2, Sparkles } from "lucide-react";
import { aiFeatureList } from "@/lib/landing-data";

export function AIFeaturesSection() {
  return (
    <section className="bg-background py-16 sm:py-24">
      <div className="mx-auto grid max-w-7xl gap-10 px-4 sm:px-6 lg:grid-cols-[0.95fr_1.05fr] lg:items-center lg:px-8">
        <div>
          <span className="inline-flex items-center gap-1.5 rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-700">
            <Sparkles className="h-3.5 w-3.5" />
            AI-Powered
          </span>
          <h2 className="mt-4 text-3xl font-bold tracking-tight sm:text-4xl">
            Xây dựng dự án tốt hơn với AI
          </h2>
          <p className="mt-4 text-base leading-7 text-muted-foreground">
            AI đóng vai trò trợ lý, giúp người dùng trình bày và đánh giá dự án
            tốt hơn. Quyết định cuối cùng luôn thuộc về con người.
          </p>
          <div className="mt-8 grid gap-3 sm:grid-cols-2">
            {aiFeatureList.map((feature) => (
              <div
                key={feature}
                className="flex items-center gap-2.5 text-sm"
              >
                <CheckCircle2 className="h-4 w-4 shrink-0 text-emerald-600" />
                <span>{feature}</span>
              </div>
            ))}
          </div>
        </div>

        {/* AI Mockup */}
        <div className="rounded-2xl border border-border/60 bg-card p-6 shadow-xl">
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-sm font-medium text-muted-foreground">
                Project Quality Score
              </p>
              <p className="mt-2 text-4xl font-bold">
                <span className="bg-gradient-to-r from-primary to-blue-500 bg-clip-text text-transparent">
                  82
                </span>
                <span className="text-lg text-muted-foreground">/100</span>
              </p>
            </div>
            {/* Circular progress */}
            <div className="relative flex h-20 w-20 items-center justify-center">
              <svg className="h-20 w-20 -rotate-90" viewBox="0 0 80 80">
                <circle
                  cx="40"
                  cy="40"
                  r="34"
                  fill="none"
                  stroke="hsl(var(--muted))"
                  strokeWidth="6"
                />
                <circle
                  cx="40"
                  cy="40"
                  r="34"
                  fill="none"
                  stroke="hsl(var(--primary))"
                  strokeWidth="6"
                  strokeLinecap="round"
                  strokeDasharray={`${82 * 2.136} ${(100 - 82) * 2.136}`}
                />
              </svg>
              <span className="absolute text-lg font-bold text-primary">
                82
              </span>
            </div>
          </div>

          {/* Progress bar */}
          <div className="mt-6 h-2.5 overflow-hidden rounded-full bg-muted">
            <div className="h-2.5 w-[82%] rounded-full bg-gradient-to-r from-primary to-blue-500" />
          </div>

          <div className="mt-6 grid gap-4 md:grid-cols-2">
            <InsightBlock
              title="Điểm mạnh"
              tone="success"
              items={[
                "Mục tiêu dự án rõ ràng",
                "Đối tượng người dùng cụ thể",
              ]}
            />
            <InsightBlock
              title="Gợi ý cải thiện"
              tone="warning"
              items={[
                "Bổ sung kế hoạch MVP",
                "Làm rõ kỹ năng cần tuyển",
              ]}
            />
          </div>
        </div>
      </div>
    </section>
  );
}

function InsightBlock({
  title,
  items,
  tone,
}: {
  title: string;
  items: string[];
  tone: "success" | "warning";
}) {
  const colors =
    tone === "success"
      ? "bg-emerald-50 border-emerald-100"
      : "bg-amber-50 border-amber-100";
  const dotColor =
    tone === "success" ? "bg-emerald-500" : "bg-amber-500";

  return (
    <div className={`rounded-xl border p-4 ${colors}`}>
      <h3 className="text-sm font-bold">{title}</h3>
      <ul className="mt-3 space-y-2.5 text-sm text-muted-foreground">
        {items.map((item) => (
          <li key={item} className="flex gap-2.5">
            <span className={`mt-1.5 h-2 w-2 shrink-0 rounded-full ${dotColor}`} />
            <span>{item}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
