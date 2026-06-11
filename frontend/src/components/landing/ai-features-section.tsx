import { CheckCircle2, Sparkles } from "lucide-react";
import { aiFeatureList } from "@/lib/landing-data";

export function AIFeaturesSection() {
  return (
    <section className="bg-background py-16 sm:py-20">
      <div className="mx-auto grid max-w-7xl gap-8 px-4 sm:px-6 lg:grid-cols-[0.95fr_1.05fr] lg:px-8">
        <div>
          <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary/10 text-primary">
            <Sparkles className="h-5 w-5" />
          </div>
          <h2 className="mt-5 text-3xl font-semibold">Xây dựng dự án tốt hơn với AI</h2>
          <p className="mt-3 text-sm leading-6 text-muted-foreground">
            AI đóng vai trò trợ lý, giúp người dùng trình bày và đánh giá dự án tốt hơn. Quyết định cuối cùng luôn thuộc về con người.
          </p>
          <div className="mt-6 grid gap-3 sm:grid-cols-2">
            {aiFeatureList.map((feature) => (
              <div key={feature} className="flex items-center gap-2 text-sm">
                <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                {feature}
              </div>
            ))}
          </div>
        </div>

        <div className="rounded-2xl border border-border bg-card p-6 shadow-sm">
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-sm text-muted-foreground">Project Quality Score</p>
              <p className="mt-2 text-4xl font-semibold text-primary">82/100</p>
            </div>
            <div className="flex h-20 w-20 items-center justify-center rounded-full border-8 border-primary/20 bg-primary/10 text-lg font-semibold text-primary">
              82
            </div>
          </div>
          <div className="mt-6 h-2 rounded-full bg-muted">
            <div className="h-2 w-[82%] rounded-full bg-primary" />
          </div>
          <div className="mt-6 grid gap-4 md:grid-cols-2">
            <InsightBlock title="Điểm mạnh" items={["Mục tiêu dự án rõ ràng", "Đối tượng người dùng cụ thể"]} />
            <InsightBlock title="Gợi ý cải thiện" items={["Bổ sung kế hoạch MVP", "Làm rõ kỹ năng cần tuyển"]} />
          </div>
        </div>
      </div>
    </section>
  );
}

function InsightBlock({ title, items }: { title: string; items: string[] }) {
  return (
    <div className="rounded-xl bg-muted/60 p-4">
      <h3 className="text-sm font-semibold">{title}</h3>
      <ul className="mt-3 space-y-2 text-sm text-muted-foreground">
        {items.map((item) => (
          <li key={item} className="flex gap-2">
            <span className="mt-2 h-1.5 w-1.5 rounded-full bg-primary" />
            <span>{item}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
