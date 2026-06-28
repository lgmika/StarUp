import type { Metadata } from "next";
import { Inter } from "next/font/google";
import { Toaster } from "sonner";
import {NextIntlClientProvider} from 'next-intl';
import {getMessages} from 'next-intl/server';
import {notFound} from 'next/navigation';
import {routing} from '@/i18n/routing';
import { AppProviders } from "@/components/providers/app-providers";
import "../globals.css";
const inter = Inter({
  subsets: ["latin", "vietnamese"],
  variable: "--font-inter",
});

export const metadata: Metadata = {
  title: {
    default: "StartupConnect Console",
    template: "%s | StartupConnect",
  },
  description:
    "StartupConnect is a platform connecting founders, members, investors, and businesses to build and grow startup projects together.",
  keywords: [
    "startup",
    "founder",
    "investor",
    "connect",
    "project",
    "collaboration",
    "team building",
  ],
};

export default async function RootLayout({
  children,
  params,
}: Readonly<{
  children: React.ReactNode;
  params: Promise<{locale: string}>;
}>) {
  const { locale } = await params;
  if (!routing.locales.includes(locale as "en" | "vi")) {
    notFound();
  }

  const messages = await getMessages();

  return (
    <html lang={locale} suppressHydrationWarning>
      <body className={`${inter.variable} overflow-x-hidden font-sans antialiased`}>
        <NextIntlClientProvider messages={messages}>
          <AppProviders>
            {children}
            <Toaster position="top-right" richColors closeButton duration={4000} />
          </AppProviders>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
