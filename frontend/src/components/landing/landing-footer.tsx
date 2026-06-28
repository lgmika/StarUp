import { Link } from "@/i18n/routing";
import { useTranslations } from "next-intl";
import { Lightbulb } from "lucide-react";

export function LandingFooter() {
  const t = useTranslations("LandingFooter");

  const footerGroups = [
    {
      title: t("platform"),
      links: [
        { label: t("discover"), href: "/projects" },
        { label: t("pricing"), href: "/pricing" },
        { label: t("events"), href: "/events" },
        { label: t("marketTrends"), href: "/trends" },
      ],
    },
    {
      title: t("support"),
      links: [
        { label: t("helpCenter"), href: "/help" },
        { label: t("reportIssue"), href: "/moderator/reports" },
        { label: t("aboutUs"), href: "/about" },
      ],
    },
    {
      title: t("legal"),
      links: [
        { label: t("terms"), href: "/" },
        { label: t("privacy"), href: "/" },
        { label: t("ideaProtection"), href: "/#how-it-works" },
        { label: t("aboutNda"), href: "/nda-agreements" },
      ],
    },
  ];

  return (
    <footer className="border-t border-border bg-card">
      <div className="mx-auto grid max-w-7xl gap-10 px-4 py-12 sm:px-6 md:grid-cols-[1.3fr_2fr] lg:px-8">
        <div>
          <Link href="/" className="group flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-md shadow-primary/25 transition-transform group-hover:scale-105">
              <Lightbulb className="h-5 w-5" />
            </span>
            <span className="font-bold">StartupConnect</span>
          </Link>
          <p className="mt-4 max-w-sm text-sm leading-7 text-muted-foreground">
            {t("description")}
          </p>
        </div>
        <div className="grid gap-8 sm:grid-cols-3">
          {footerGroups.map((group) => (
            <div key={group.title}>
              <h2 className="text-sm font-bold">{group.title}</h2>
              <div className="mt-4 space-y-3">
                {group.links.map((link) => (
                  <Link
                    key={link.label}
                    href={link.href}
                    className="block text-sm text-muted-foreground transition-colors hover:text-foreground"
                  >
                    {link.label}
                  </Link>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
      <div className="border-t border-border px-4 py-5 text-center text-sm text-muted-foreground">
        © {new Date().getFullYear()} {t("allRightsReserved")}
      </div>
    </footer>
  );
}
