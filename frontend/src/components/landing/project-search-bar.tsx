"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { Search, Sparkles } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { popularKeywords, projectFields } from "@/lib/landing-data";

export function ProjectSearchBar() {
  const router = useRouter();
  const [keyword, setKeyword] = useState("");
  const [field, setField] = useState(projectFields[0]);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const params = new URLSearchParams();
    if (keyword.trim()) params.set("search", keyword.trim());
    if (field !== projectFields[0]) params.set("field", field);
    router.push(
      `/projects${params.toString() ? `?${params.toString()}` : ""}`
    );
  }

  return (
    <section className="border-b border-border bg-background">
      <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        <form
          onSubmit={handleSubmit}
          className="rounded-2xl border border-border/60 bg-card p-4 shadow-lg"
        >
          <div className="grid gap-3 lg:grid-cols-[1fr_220px_auto]">
            <label className="sr-only" htmlFor="landing-search">
              Search projects
            </label>
            <div className="relative">
              <Search className="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                id="landing-search"
                value={keyword}
                onChange={(event) => setKeyword(event.target.value)}
                className="h-12 rounded-xl pl-11 text-base"
                placeholder="Search projects, skills, or fields..."
              />
            </div>
            <label className="sr-only" htmlFor="project-field">
              Field
            </label>
            <select
              id="project-field"
              value={field}
              onChange={(event) => setField(event.target.value)}
              className="h-12 rounded-xl border border-input bg-background px-4 text-sm font-medium outline-none transition-colors focus-visible:ring-2 focus-visible:ring-ring"
            >
              {projectFields.map((item) => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>
            <Button
              className="h-12 rounded-xl px-6 shadow-md shadow-primary/20"
              type="submit"
            >
              <Search className="h-4 w-4" />
              Search
            </Button>
          </div>
        </form>
        <div className="mt-4 flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
          <span className="flex items-center gap-1.5">
            <Sparkles className="h-3.5 w-3.5 text-primary" />
            Popular:
          </span>
          {popularKeywords.map((item) => (
            <button
              key={item}
              type="button"
              className="rounded-full border border-border/60 px-3 py-1 text-xs font-medium text-foreground transition-all hover:-translate-y-0.5 hover:border-primary/30 hover:bg-primary/5 hover:text-primary"
              onClick={() => {
                setKeyword(item);
                router.push(`/projects?search=${encodeURIComponent(item)}`);
              }}
            >
              {item}
            </button>
          ))}
        </div>
      </div>
    </section>
  );
}
