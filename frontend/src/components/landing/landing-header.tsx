"use client";

import { Link, useRouter, usePathname } from "@/i18n/routing";
import { useState, useEffect } from "react";
import { useTranslations, useLocale } from "next-intl";
import { useTheme } from "next-themes";
import { Lightbulb, Menu, X, Moon, Sun } from "lucide-react";
import { PublicSessionActions } from "@/components/auth/public-session-actions";
import { cn } from "@/lib/utils";

export function LandingHeader() {
  const t = useTranslations("LandingHeader");
  const navItems = [
    { label: t("discover"), href: "/projects" },
    { label: t("pricing"), href: "/pricing" },
    { label: t("forFounders"), href: "/#founder" },
    { label: t("forInvestors"), href: "/#investor" },
  ];
  const [isOpen, setIsOpen] = useState(false);
  const router = useRouter();
  const pathname = usePathname();
  const locale = useLocale();
  const { resolvedTheme, setTheme } = useTheme();
  const [mounted, setMounted] = useState(false);
  
  useEffect(() => {
    setMounted(true);
  }, []);

  return (
    <header className="sticky top-0 z-40 border-b border-border/60 bg-background/80 backdrop-blur-xl">
      <div className="mx-auto flex h-16 w-full max-w-7xl items-center justify-between gap-3 px-4 sm:px-6 lg:px-8">
        <Link
          href="/"
          className="group flex min-w-0 items-center gap-3 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        >
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-md shadow-primary/25 transition-transform group-hover:scale-105">
            <Lightbulb className="h-4.5 w-4.5" />
          </span>
          <span className="truncate text-base font-bold">StartupConnect</span>
        </Link>

        <nav className="hidden items-center gap-1 text-sm font-medium text-muted-foreground lg:flex">
          {navItems.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className="rounded-lg px-3 py-2 transition-colors hover:bg-accent hover:text-foreground"
            >
              {item.label}
            </Link>
          ))}
        </nav>

        <div className="hidden lg:flex lg:items-center lg:gap-3">
          <div className="flex items-center gap-2 border-r border-border pr-3">
            <button
              type="button"
              aria-label={mounted && resolvedTheme === "dark" ? "Use light theme" : "Use dark theme"}
              className="flex h-9 w-9 items-center justify-center rounded-md border border-border hover:bg-accent"
              onClick={() => setTheme(resolvedTheme === "dark" ? "light" : "dark")}
            >
              {mounted && resolvedTheme === "dark" ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
            </button>
            <div className="flex h-9 items-center rounded-md border border-border bg-muted p-0.5">
               <Link href={pathname || "/"} locale="en" className={cn("px-2 py-1 text-xs rounded-sm transition-colors", locale === 'en' && "bg-background shadow-sm")}>EN</Link>
               <Link href={pathname || "/"} locale="vi" className={cn("px-2 py-1 text-xs rounded-sm transition-colors", locale === 'vi' && "bg-background shadow-sm")}>VI</Link>
            </div>
          </div>
          <PublicSessionActions />
        </div>

        <button
          type="button"
          className="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-border transition-colors hover:bg-accent lg:hidden"
          aria-label={isOpen ? "Close menu" : "Open menu"}
          aria-expanded={isOpen}
          onClick={() => setIsOpen((value) => !value)}
        >
          {isOpen ? (
            <X className="h-5 w-5" />
          ) : (
            <Menu className="h-5 w-5" />
          )}
        </button>
      </div>

      {isOpen ? (
        <div className="border-t border-border/60 bg-background/95 px-4 py-4 shadow-lg backdrop-blur-xl lg:hidden">
          <nav className="mx-auto flex max-w-7xl flex-col gap-1">
            {navItems.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className="rounded-lg px-3 py-2.5 text-sm font-medium text-muted-foreground hover:bg-accent hover:text-foreground"
                onClick={() => setIsOpen(false)}
              >
                {item.label}
              </Link>
            ))}
            <div className="mt-3 border-t border-border pt-3">
              <div className="mb-4 flex items-center justify-between">
                <span className="text-sm font-medium text-muted-foreground">{t('theme')}</span>
                <button
                  type="button"
                  aria-label={mounted && resolvedTheme === "dark" ? "Use light theme" : "Use dark theme"}
                  className="flex h-9 w-9 items-center justify-center rounded-md border border-border hover:bg-accent"
                  onClick={() => setTheme(resolvedTheme === "dark" ? "light" : "dark")}
                >
                  {mounted && resolvedTheme === "dark" ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
                </button>
              </div>
              <div className="mb-4 flex items-center justify-between">
                <span className="text-sm font-medium text-muted-foreground">{t('language')}</span>
                <div className="flex h-9 items-center rounded-md border border-border bg-muted p-0.5">
                   <Link href={pathname || "/"} locale="en" className={cn("px-2 py-1.5 text-xs rounded-sm transition-colors", locale === 'en' && "bg-background shadow-sm")}>EN</Link>
                   <Link href={pathname || "/"} locale="vi" className={cn("px-2 py-1.5 text-xs rounded-sm transition-colors", locale === 'vi' && "bg-background shadow-sm")}>VI</Link>
                </div>
              </div>
              <PublicSessionActions />
            </div>
          </nav>
        </div>
      ) : null}
    </header>
  );
}
