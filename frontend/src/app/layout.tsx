import type { Metadata } from "next";
import { Inter } from "next/font/google";
import { Toaster } from "sonner";
import { AppProviders } from "@/components/providers/app-providers";
import "./globals.css";

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

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className={`${inter.variable} overflow-x-hidden font-sans antialiased`}>
        <AppProviders>
          {children}
          <Toaster position="top-right" richColors closeButton duration={4000} />
        </AppProviders>
      </body>
    </html>
  );
}
