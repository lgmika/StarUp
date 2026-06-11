import Link from "next/link";
import { ArrowRight } from "lucide-react";

export function FinalCTASection() {
  return (
    <section className="bg-background py-16 sm:py-20">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="rounded-2xl bg-primary px-6 py-10 text-primary-foreground shadow-sm sm:px-10 lg:px-14">
          <div className="flex flex-col justify-between gap-8 lg:flex-row lg:items-center">
            <div className="max-w-2xl">
              <h2 className="text-3xl font-semibold">Ý tưởng của bạn có thể là khởi đầu của một sản phẩm thực sự</h2>
              <p className="mt-3 text-sm leading-6 text-primary-foreground/85">
                Hãy tìm những người phù hợp để cùng biến ý tưởng đó thành hiện thực.
              </p>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row">
              <Link href="/projects/create" className="inline-flex h-11 items-center justify-center gap-2 rounded-md bg-background px-5 text-sm font-medium text-foreground hover:bg-background/90">
                Tạo dự án miễn phí
                <ArrowRight className="h-4 w-4" />
              </Link>
              <Link href="/projects" className="inline-flex h-11 items-center justify-center rounded-md border border-primary-foreground/30 px-5 text-sm font-medium hover:bg-primary-foreground/10">
                Khám phá dự án
              </Link>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
