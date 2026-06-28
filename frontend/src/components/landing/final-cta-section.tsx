import Link from "next/link";
import { ArrowRight, Sparkles } from "lucide-react";

export function FinalCTASection() {
  return (
    <section className="bg-background py-16 sm:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-primary via-primary to-blue-600 px-6 py-14 text-primary-foreground shadow-2xl shadow-primary/20 sm:px-10 lg:px-16">
          {/* Background decoration */}
          <div className="pointer-events-none absolute inset-0">
            <div className="absolute -right-24 -top-24 h-64 w-64 rounded-full bg-white/5 blur-2xl" />
            <div className="absolute -bottom-24 -left-24 h-64 w-64 rounded-full bg-white/5 blur-2xl" />
            <div
              className="absolute inset-0 opacity-[0.07]"
              style={{
                backgroundImage: `url("data:image/svg+xml,%3Csvg width='40' height='40' viewBox='0 0 40 40' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='%23fff' fill-opacity='1' fill-rule='evenodd'%3E%3Cpath d='m0 40 40-40h-4L0 36v4zm40-2L2 0H0v2l40 38v-2z'/%3E%3C/g%3E%3C/svg%3E")`,
              }}
            />
          </div>

          <div className="relative flex flex-col justify-between gap-8 lg:flex-row lg:items-center">
            <div className="max-w-2xl">
              <div className="flex items-center gap-2 text-sm font-semibold text-primary-foreground/80">
                <Sparkles className="h-4 w-4" />
                Bắt đầu miễn phí
              </div>
              <h2 className="mt-4 text-3xl font-bold tracking-tight sm:text-4xl">
                Ý tưởng của bạn có thể là khởi đầu của một sản phẩm thực sự
              </h2>
              <p className="mt-4 text-base leading-7 text-primary-foreground/80">
                Hãy tìm những người phù hợp để cùng biến ý tưởng đó thành hiện
                thực. Tham gia cùng hàng ngàn founder, member và investor trên
                StartupConnect.
              </p>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row lg:flex-col">
              <Link
                href="/projects/create"
                className="group inline-flex h-12 items-center justify-center gap-2 rounded-xl bg-white px-6 text-sm font-semibold text-primary shadow-lg transition-all hover:-translate-y-0.5 hover:bg-white/95 hover:shadow-xl"
              >
                Tạo dự án miễn phí
                <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
              </Link>
              <Link
                href="/projects"
                className="inline-flex h-12 items-center justify-center rounded-xl border border-primary-foreground/30 px-6 text-sm font-semibold transition-all hover:-translate-y-0.5 hover:border-primary-foreground/50 hover:bg-primary-foreground/10"
              >
                Khám phá dự án
              </Link>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
