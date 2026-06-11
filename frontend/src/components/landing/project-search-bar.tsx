"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { Search } from "lucide-react";
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
    router.push(`/projects${params.toString() ? `?${params.toString()}` : ""}`);
  }

  return (
    <section className="border-b border-border bg-background">
      <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
        <form onSubmit={handleSubmit} className="rounded-2xl border border-border bg-card p-3 shadow-sm">
          <div className="grid gap-3 lg:grid-cols-[1fr_220px_auto]">
            <label className="sr-only" htmlFor="landing-search">
              Tìm dự án
            </label>
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                id="landing-search"
                value={keyword}
                onChange={(event) => setKeyword(event.target.value)}
                className="h-12 pl-10"
                placeholder="Tìm dự án, kỹ năng hoặc lĩnh vực..."
              />
            </div>
            <label className="sr-only" htmlFor="project-field">
              Lĩnh vực
            </label>
            <select
              id="project-field"
              value={field}
              onChange={(event) => setField(event.target.value)}
              className="h-12 rounded-md border border-input bg-background px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              {projectFields.map((item) => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>
            <Button className="h-12 px-6" type="submit">
              <Search className="h-4 w-4" />
              Tìm kiếm
            </Button>
          </div>
        </form>
        <div className="mt-3 flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
          <span>Từ khóa phổ biến:</span>
          {popularKeywords.map((item) => (
            <button
              key={item}
              type="button"
              className="rounded-full border border-border px-3 py-1 text-xs font-medium text-foreground transition-colors hover:bg-accent"
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
