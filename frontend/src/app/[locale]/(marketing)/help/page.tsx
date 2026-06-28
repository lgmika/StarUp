"use client";

import { LifeBuoy, MessageCircle, FileText, Search, ChevronDown } from "lucide-react";

import { Button } from "@/components/ui/button";

const FAQS = [
  {
    question: "How do I create a new startup project?",
    answer: "Once you sign in, click on the 'Create Project' button in your dashboard. Fill in the required details including your project's vision, required roles, and visibility settings.",
  },
  {
    question: "Is StartupConnect free for founders?",
    answer: "Yes, you can create a basic profile and join projects for free. We also offer a Founder Pro plan for $19/month that unlocks unlimited applications, AI reviews, and investor contact features.",
  },
  {
    question: "How are NDA agreements handled?",
    answer: "If a project has 'NDA Required' visibility, interested parties must sign a digital NDA template provided by the founder before they can view sensitive details. This is all handled securely within the platform.",
  },
  {
    question: "Can I invest in projects through the platform?",
    answer: "Currently, StartupConnect facilitates the connection and communication between investors and founders. The actual financial transactions and legal investments happen off-platform.",
  },
  {
    question: "How does the AI Review system work?",
    answer: "Our AI model analyzes your project's description, market fit, and team completeness to provide a quality score. It identifies risk flags and suggests actionable improvements to make your project more appealing to investors.",
  },
];

export default function HelpCenterPage() {
  return (
    <main className="bg-background">
      
      {/* Hero Section */}
      <div className="relative overflow-hidden bg-primary/5 py-24 sm:py-32">
        <div className="mx-auto max-w-7xl px-6 lg:px-8 text-center">
          <h1 className="text-4xl font-bold tracking-tight text-foreground sm:text-6xl">
            How can we help?
          </h1>
          <p className="mt-6 text-lg leading-8 text-muted-foreground">
            Search our knowledge base or browse categories below to find answers.
          </p>
          <div className="mx-auto mt-10 max-w-xl">
            <div className="relative flex items-center">
              <Search className="absolute left-4 h-5 w-5 text-muted-foreground" />
              <input
                type="text"
                placeholder="Search for articles, guides, or questions..."
                className="w-full rounded-full border border-border bg-background py-4 pl-12 pr-4 text-sm shadow-sm outline-none transition-colors focus:border-primary focus:ring-1 focus:ring-primary"
              />
            </div>
          </div>
        </div>
      </div>

      {/* Categories */}
      <div className="mx-auto max-w-7xl px-6 py-16 lg:px-8">
        <div className="grid gap-8 md:grid-cols-3">
          <div className="rounded-3xl border border-border/50 bg-card p-8 shadow-sm transition-shadow hover:shadow-md text-center">
            <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
              <FileText className="h-6 w-6" />
            </div>
            <h3 className="mt-4 text-lg font-semibold">Getting Started</h3>
            <p className="mt-2 text-sm text-muted-foreground">Learn the basics of setting up your profile and finding projects.</p>
          </div>
          <div className="rounded-3xl border border-border/50 bg-card p-8 shadow-sm transition-shadow hover:shadow-md text-center">
            <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-blue-500/10 text-blue-600">
              <LifeBuoy className="h-6 w-6" />
            </div>
            <h3 className="mt-4 text-lg font-semibold">Account & Billing</h3>
            <p className="mt-2 text-sm text-muted-foreground">Manage your subscription, payments, and account security.</p>
          </div>
          <div className="rounded-3xl border border-border/50 bg-card p-8 shadow-sm transition-shadow hover:shadow-md text-center">
            <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-emerald-500/10 text-emerald-600">
              <MessageCircle className="h-6 w-6" />
            </div>
            <h3 className="mt-4 text-lg font-semibold">Contact Support</h3>
            <p className="mt-2 text-sm text-muted-foreground">Can&apos;t find what you&apos;re looking for? Reach out to our team.</p>
          </div>
        </div>
      </div>

      {/* FAQs */}
      <div className="mx-auto max-w-4xl px-6 py-16 lg:px-8">
        <h2 className="text-2xl font-bold tracking-tight text-center mb-12">Frequently Asked Questions</h2>
        <div className="space-y-4">
          {FAQS.map((faq, index) => (
            <details
              key={index}
              className="group rounded-2xl border border-border/50 bg-card p-6 [&_summary::-webkit-details-marker]:hidden"
            >
              <summary className="flex cursor-pointer items-center justify-between gap-1.5 text-foreground font-semibold">
                {faq.question}
                <ChevronDown className="h-5 w-5 shrink-0 transition duration-300 group-open:-rotate-180" />
              </summary>
              <p className="mt-4 text-sm leading-relaxed text-muted-foreground">
                {faq.answer}
              </p>
            </details>
          ))}
        </div>
      </div>

      {/* CTA */}
      <div className="mx-auto max-w-7xl px-6 py-16 lg:px-8">
        <div className="rounded-3xl bg-primary px-6 py-12 text-center sm:p-16">
          <h2 className="text-2xl font-bold tracking-tight text-primary-foreground sm:text-3xl">
            Still have questions?
          </h2>
          <p className="mx-auto mt-4 max-w-xl text-primary-foreground/80">
            Our support team is available 24/7 to help you with any issues or questions you might have.
          </p>
          <div className="mt-8 flex justify-center gap-4">
            <Button variant="secondary" size="md" className="rounded-full">
              Chat with us
            </Button>
            <Button variant="outline" size="md" className="rounded-full bg-transparent text-primary-foreground border-primary-foreground/20 hover:bg-primary-foreground/10 hover:text-primary-foreground">
              Send an email
            </Button>
          </div>
        </div>
      </div>

    </main>
  );
}
