"use client";

import Link from "next/link";
import { ArrowRight, CheckCircle2, Sparkles, UsersRound, WalletCards } from "lucide-react";
import { Badge } from "@/components/ui/badge";

export function HeroSection() {
  return (
    <section className="relative max-w-full overflow-hidden border-b border-border bg-gradient-to-b from-background via-background to-muted/40">
      {/* Animated background blobs */}
      <div className="pointer-events-none absolute inset-0 overflow-hidden">
        <div className="absolute -left-32 -top-32 h-96 w-96 animate-[pulse_8s_ease-in-out_infinite] rounded-full bg-primary/5 blur-3xl" />
        <div className="absolute -bottom-32 -right-32 h-96 w-96 animate-[pulse_10s_ease-in-out_infinite_2s] rounded-full bg-primary/5 blur-3xl" />
        <div className="absolute left-1/2 top-1/3 h-64 w-64 animate-[pulse_12s_ease-in-out_infinite_4s] rounded-full bg-primary/3 blur-3xl" />
        {/* Grid pattern overlay */}
        <div
          className="absolute inset-0 opacity-[0.015]"
          style={{
            backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23000' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
          }}
        />
      </div>

      <div className="relative mx-auto grid w-full max-w-7xl gap-10 overflow-hidden px-4 py-16 sm:px-6 lg:grid-cols-[1.02fr_0.98fr] lg:px-8 lg:py-24">
        <div className="flex min-w-0 flex-col justify-center">
          <Badge tone="default" className="w-fit animate-fade-in">
            Nền tảng kết nối cộng đồng khởi nghiệp
          </Badge>
          <h1 className="mt-6 max-w-[calc(100vw-2rem)] break-words text-3xl font-bold leading-tight tracking-tight text-foreground sm:max-w-3xl sm:text-5xl lg:text-6xl">
            Biến ý tưởng khởi nghiệp thành một{" "}
            <span className="bg-gradient-to-r from-primary via-primary to-blue-500 bg-clip-text text-transparent">
              dự án thực tế
            </span>
          </h1>
          <p className="mt-5 max-w-[calc(100vw-2rem)] text-base leading-7 text-muted-foreground sm:max-w-2xl sm:text-lg">
            Kết nối với cộng sự, chuyên gia, doanh nghiệp và nhà đầu tư phù
            hợp để cùng xây dựng sản phẩm từ ý tưởng ban đầu.
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <Link
              href="/projects/create"
              className="group inline-flex h-12 items-center justify-center gap-2 rounded-xl bg-primary px-6 text-sm font-semibold text-primary-foreground shadow-lg shadow-primary/25 transition-all hover:-translate-y-0.5 hover:bg-primary/90 hover:shadow-xl hover:shadow-primary/30"
            >
              Tạo dự án của bạn
              <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
            </Link>
            <Link
              href="/projects"
              className="inline-flex h-12 items-center justify-center gap-2 rounded-xl border border-border bg-background px-6 text-sm font-semibold transition-all hover:-translate-y-0.5 hover:bg-accent hover:shadow-md"
            >
              Khám phá dự án
            </Link>
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
      {/* Floating investor card */}
      <div className="absolute -right-4 top-6 z-10 hidden animate-fade-in rounded-2xl border border-border/60 bg-card/95 p-4 shadow-xl backdrop-blur-sm sm:block">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-100 text-emerald-600">
            <WalletCards className="h-5 w-5" />
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Investor</p>
            <p className="text-sm font-semibold text-emerald-600">Interested</p>
          </div>
        </div>
      </div>

      {/* Main mockup card */}
      <div className="max-w-full overflow-hidden rounded-2xl border border-border/60 bg-card/80 p-5 shadow-2xl backdrop-blur-sm">
        <div className="max-w-full overflow-hidden rounded-xl border border-border/40 bg-gradient-to-br from-muted/50 to-background p-4">
          <div className="flex items-start justify-between gap-4">
            <div>
              <Badge tone="success">GreenTech</Badge>
              <h2 className="mt-4 text-2xl font-bold">EcoTrack</h2>
              <p className="mt-2 text-sm leading-6 text-muted-foreground">
                Nền tảng giúp doanh nghiệp theo dõi và giảm lượng phát thải.
              </p>
            </div>
            <div className="rounded-xl bg-background p-3 text-center shadow-sm ring-1 ring-border/50">
              <p className="text-xs text-muted-foreground">Quality</p>
              <p className="text-2xl font-bold text-primary">86</p>
            </div>
          </div>

          {/* Animated progress bar */}
          <div className="mt-5 h-2.5 overflow-hidden rounded-full bg-background">
            <div className="h-2.5 w-[86%] rounded-full bg-gradient-to-r from-primary to-blue-500 transition-all duration-1000" />
          </div>

          <div className="mt-6 grid gap-3 sm:grid-cols-2">
            <div className="rounded-xl bg-background p-4 ring-1 ring-border/30">
              <p className="text-sm font-semibold">Đang tuyển</p>
              <div className="mt-3 space-y-2.5 text-sm text-muted-foreground">
                <RoleRow label="Backend Developer" />
                <RoleRow label="UI/UX Designer" />
                <RoleRow label="Business Analyst" />
              </div>
            </div>
            <div className="rounded-xl bg-background p-4 ring-1 ring-border/30">
              <p className="text-sm font-semibold">Team signal</p>
              <div className="mt-4 flex -space-x-2">
                {[
                  { initials: "EC", color: "bg-blue-100 text-blue-700" },
                  { initials: "UX", color: "bg-purple-100 text-purple-700" },
                  { initials: "AI", color: "bg-amber-100 text-amber-700" },
                  { initials: "BA", color: "bg-emerald-100 text-emerald-700" },
                ].map((item) => (
                  <span
                    key={item.initials}
                    className={`flex h-9 w-9 items-center justify-center rounded-full border-2 border-card text-xs font-semibold ${item.color}`}
                  >
                    {item.initials}
                  </span>
                ))}
              </div>
              <div className="mt-4 flex items-center gap-2 text-sm text-muted-foreground">
                <UsersRound className="h-4 w-4" />4 thành viên đang xây dựng
                MVP
              </div>
            </div>
          </div>

          <div className="mt-4 flex items-center gap-2 rounded-xl bg-gradient-to-r from-primary/10 to-blue-500/10 p-3 text-sm text-primary">
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
