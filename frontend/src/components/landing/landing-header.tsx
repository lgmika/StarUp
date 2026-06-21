"use client";

import Link from "next/link";
import { useState } from "react";
import { Lightbulb, Menu, X } from "lucide-react";
import { PublicSessionActions } from "@/components/auth/public-session-actions";

const navItems = [
  { label: "Discover projects", href: "/projects" },
  { label: "For founders", href: "/#founder" },
  { label: "For investors", href: "/#investor" },
  { label: "How it works", href: "/#how-it-works" },
];

export function LandingHeader() {
  const [isOpen, setIsOpen] = useState(false);
  return <header className="sticky top-0 z-40 border-b border-border bg-background/95 backdrop-blur">
    <div className="mx-auto flex h-16 w-full max-w-7xl items-center justify-between gap-3 px-4 sm:px-6 lg:px-8">
      <Link href="/" className="flex min-w-0 items-center gap-3 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"><span className="flex h-10 w-10 items-center justify-center rounded-md bg-primary text-primary-foreground"><Lightbulb className="h-5 w-5" /></span><span className="truncate text-base font-semibold">StartupConnect</span></Link>
      <nav className="hidden items-center gap-6 text-sm font-medium text-muted-foreground lg:flex">{navItems.map((item) => <Link key={item.href} href={item.href} className="transition-colors hover:text-foreground">{item.label}</Link>)}</nav>
      <div className="hidden lg:block"><PublicSessionActions /></div>
      <button type="button" className="inline-flex h-10 w-10 items-center justify-center rounded-md border border-border lg:hidden" aria-label={isOpen ? "Close menu" : "Open menu"} aria-expanded={isOpen} onClick={() => setIsOpen((value) => !value)}>{isOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}</button>
    </div>
    {isOpen ? <div className="border-t border-border bg-background px-4 py-4 shadow-sm lg:hidden"><nav className="mx-auto flex max-w-7xl flex-col gap-2">{navItems.map((item) => <Link key={item.href} href={item.href} className="rounded-md px-3 py-2 text-sm font-medium text-muted-foreground hover:bg-accent" onClick={() => setIsOpen(false)}>{item.label}</Link>)}<div className="mt-3 border-t border-border pt-3"><PublicSessionActions /></div></nav></div> : null}
  </header>;
}
