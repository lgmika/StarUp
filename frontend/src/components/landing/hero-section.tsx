import Link from "next/link";
import { ArrowRight, CheckCircle2, Sparkles, UsersRound, WalletCards } from "lucide-react";
import { Badge } from "@/components/ui/badge";

export function HeroSection() {
  return (
    <section className="relative max-w-full overflow-hidden border-b border-border bg-gradient-to-b from-background to-muted/40">
      <div className="mx-auto grid w-full max-w-7xl gap-10 overflow-hidden px-4 py-16 sm:px-6 lg:grid-cols-[1.02fr_0.98fr] lg:px-8 lg:py-20">
        <div className="min-w-0 flex flex-col justify-center">
          <Badge tone="default" className="w-fit">
            Nền tảng kết nối cộng đồng khởi nghiệp
          </Badge>
          <h1 className="mt-6 max-w-[calc(100vw-2rem)] break-words text-3xl font-semibold leading-tight text-foreground sm:max-w-3xl sm:text-5xl lg:text-6xl">
            Biến ý tưởng khởi nghiệp thành một{" "}
            <span className="text-primary">dự án thực tế</span>
          </h1>
          <p className="mt-5 max-w-[calc(100vw-2rem)] text-base leading-7 text-muted-foreground sm:max-w-2xl sm:text-lg">
            Kết nối với cộng sự, chuyên gia, doanh nghiệp và nhà đầu tư phù hợp để cùng xây dựng sản phẩm từ ý tưởng ban đầu.
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <LandingButton href="/projects/create" label="Tạo dự án của bạn" icon={<ArrowRight className="h-4 w-4" />} />
            <LandingButton href="/projects" label="Khám phá dự án" variant="outline" />
          </div>
          <div className="mt-6 flex flex-wrap gap-x-4 gap-y-2 text-sm text-muted-foreground">
            <TrustItem label="AI hỗ trợ hoàn thiện dự án" />
            <TrustItem label="Kiểm duyệt nội dung" />
            <TrustItem label="Bảo vệ ý tưởng" />
          </div>
        </div>

        <ProjectMockup />
      </div>
    </section>
  );
}

function LandingButton({
  href,
  label,
  variant = "primary",
  icon,
}: {
  href: string;
  label: string;
  variant?: "primary" | "outline";
  icon?: React.ReactNode;
}) {
  return (
    <Link
      href={href}
      className={
        variant === "primary"
          ? "inline-flex h-11 items-center justify-center gap-2 rounded-md bg-primary px-5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
          : "inline-flex h-11 items-center justify-center gap-2 rounded-md border border-border bg-background px-5 text-sm font-medium transition-colors hover:bg-accent"
      }
    >
      {label}
      {icon}
    </Link>
  );
}

function TrustItem({ label }: { label: string }) {
  return (
    <span className="inline-flex items-center gap-2">
      <CheckCircle2 className="h-4 w-4 text-emerald-600" />
      {label}
    </span>
  );
}

function ProjectMockup() {
  return (
    <div className="relative min-w-0 max-w-full">
      <div className="absolute -right-6 top-8 hidden rounded-2xl border border-border bg-card p-4 shadow-lg sm:block">
        <div className="flex items-center gap-3">
          <WalletCards className="h-5 w-5 text-primary" />
          <div>
            <p className="text-xs text-muted-foreground">Investor</p>
            <p className="text-sm font-semibold">Interested</p>
          </div>
        </div>
      </div>
      <div className="max-w-full overflow-hidden rounded-2xl border border-border bg-card p-5 shadow-xl">
        <div className="max-w-full overflow-hidden rounded-xl border border-border bg-muted/50 p-4">
          <div className="flex items-start justify-between gap-4">
            <div>
              <Badge tone="success">GreenTech</Badge>
              <h2 className="mt-4 text-2xl font-semibold">EcoTrack</h2>
              <p className="mt-2 text-sm leading-6 text-muted-foreground">
                Nền tảng giúp doanh nghiệp theo dõi và giảm lượng phát thải.
              </p>
            </div>
            <div className="rounded-xl bg-background p-3 text-center shadow-sm">
              <p className="text-xs text-muted-foreground">Quality</p>
              <p className="text-2xl font-semibold text-primary">86</p>
            </div>
          </div>

          <div className="mt-5 h-2 rounded-full bg-background">
            <div className="h-2 w-[86%] rounded-full bg-primary" />
          </div>

          <div className="mt-6 grid gap-3 sm:grid-cols-2">
            <div className="rounded-xl bg-background p-4">
              <p className="text-sm font-semibold">Đang tuyển</p>
              <div className="mt-3 space-y-2 text-sm text-muted-foreground">
                <RoleRow label="Backend Developer" />
                <RoleRow label="UI/UX Designer" />
                <RoleRow label="Business Analyst" />
              </div>
            </div>
            <div className="rounded-xl bg-background p-4">
              <p className="text-sm font-semibold">Team signal</p>
              <div className="mt-4 flex -space-x-2">
                {["EC", "UX", "AI", "BA"].map((item) => (
                  <span key={item} className="flex h-9 w-9 items-center justify-center rounded-full border-2 border-card bg-primary/10 text-xs font-semibold text-primary">
                    {item}
                  </span>
                ))}
              </div>
              <div className="mt-4 flex items-center gap-2 text-sm text-muted-foreground">
                <UsersRound className="h-4 w-4" />
                4 thành viên đang xây dựng MVP
              </div>
            </div>
          </div>

          <div className="mt-4 flex items-center gap-2 rounded-xl bg-primary/10 p-3 text-sm text-primary">
            <Sparkles className="h-4 w-4" />
            AI gợi ý bổ sung kế hoạch MVP và vai trò cần tuyển.
          </div>
        </div>
      </div>
    </div>
  );
}

function RoleRow({ label }: { label: string }) {
  return (
    <div className="flex items-center gap-2">
      <span className="h-1.5 w-1.5 rounded-full bg-primary" />
      {label}
    </div>
  );
}
