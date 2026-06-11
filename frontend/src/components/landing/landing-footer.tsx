import Link from "next/link";
import { Lightbulb } from "lucide-react";

const footerGroups = [
  {
    title: "Sản phẩm",
    links: [
      { label: "Khám phá dự án", href: "/projects" },
      { label: "Tạo dự án", href: "/projects/create" },
      { label: "Dành cho Member", href: "/projects" },
      { label: "Dành cho Investor", href: "/#investor" },
    ],
  },
  {
    title: "Hỗ trợ",
    links: [
      { label: "Cách hoạt động", href: "/#how-it-works" },
      { label: "Trung tâm trợ giúp", href: "/dashboard" },
      { label: "Báo cáo vi phạm", href: "/moderator/reports" },
      { label: "Liên hệ", href: "/profile" },
    ],
  },
  {
    title: "Pháp lý",
    links: [
      { label: "Điều khoản sử dụng", href: "/" },
      { label: "Chính sách quyền riêng tư", href: "/" },
      { label: "Bảo mật ý tưởng", href: "/#how-it-works" },
      { label: "Thông tin về NDA", href: "/nda-agreements" },
    ],
  },
];

export function LandingFooter() {
  return (
    <footer className="border-t border-border bg-card">
      <div className="mx-auto grid max-w-7xl gap-8 px-4 py-10 sm:px-6 md:grid-cols-[1.3fr_2fr] lg:px-8">
        <div>
          <Link href="/" className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary text-primary-foreground">
              <Lightbulb className="h-5 w-5" />
            </span>
            <span className="font-semibold">StartupConnect</span>
          </Link>
          <p className="mt-4 max-w-sm text-sm leading-6 text-muted-foreground">
            Nền tảng kết nối cộng đồng khởi nghiệp.
          </p>
        </div>
        <div className="grid gap-8 sm:grid-cols-3">
          {footerGroups.map((group) => (
            <div key={group.title}>
              <h2 className="text-sm font-semibold">{group.title}</h2>
              <div className="mt-3 space-y-2">
                {group.links.map((link) => (
                  <Link key={link.label} href={link.href} className="block text-sm text-muted-foreground hover:text-foreground">
                    {link.label}
                  </Link>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
      <div className="border-t border-border px-4 py-5 text-center text-sm text-muted-foreground">
        © 2026 StartupConnect. All rights reserved.
      </div>
    </footer>
  );
}
