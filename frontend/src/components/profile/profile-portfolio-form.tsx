"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, Plus, ExternalLink } from "lucide-react";
import { toast } from "sonner";
import { useForm } from "react-hook-form";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { FormField } from "@/components/auth/form-field";
import { Button } from "@/components/ui/button";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { portfolioSchema, type PortfolioFormValues } from "@/lib/validations/profile";
import { profileService } from "@/services";
import type { ProfileDto } from "@/types/user";

export function ProfilePortfolioForm({ profile }: { profile: ProfileDto | null }) {
  const queryClient = useQueryClient();

  const form = useForm<PortfolioFormValues>({
    resolver: zodResolver(portfolioSchema),
    defaultValues: { title: "", url: "", description: "" },
  });

  const mutation = useMutation({
    mutationFn: async (values: PortfolioFormValues) => {
      return profileService.createPortfolio({
        title: values.title,
        url: values.url,
        description: values.description?.trim() || undefined,
      });
    },
    onSuccess: () => {
      toast.success("Portfolio added.");
      form.reset({ title: "", url: "", description: "" });
      void queryClient.invalidateQueries({ queryKey: ["profile", "me"] });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  return (
    <Panel>
      <PanelHeader>
        <PanelTitle>Portfolio</PanelTitle>
      </PanelHeader>
      <PanelBody className="space-y-6">
        <form
          className="grid gap-4 md:grid-cols-2"
          onSubmit={form.handleSubmit((v) => mutation.mutate(v))}
        >
          <FormField
            label="Title"
            error={form.formState.errors.title}
            registration={form.register("title")}
          />
          <FormField
            label="URL"
            error={form.formState.errors.url}
            registration={form.register("url")}
          />
          <div className="md:col-span-2">
            <FormField
              label="Description (Optional)"
              error={form.formState.errors.description}
              registration={form.register("description")}
            />
          </div>
          <div className="md:col-span-2">
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
              Add portfolio
            </Button>
          </div>
        </form>

        {profile?.portfolios.length ? (
          <div className="grid gap-4 md:grid-cols-2">
            {profile.portfolios.map((portfolio) => (
              <a
                key={portfolio.id}
                className="group relative flex flex-col justify-between rounded-xl border border-border/60 bg-card p-4 transition-all duration-300 hover:-translate-y-1 hover:shadow-md"
                href={portfolio.url}
                rel="noreferrer"
                target="_blank"
              >
                <div>
                  <div className="flex items-start justify-between gap-2">
                    <p className="font-semibold">{portfolio.title}</p>
                    <ExternalLink className="h-4 w-4 shrink-0 text-muted-foreground opacity-50 transition-opacity group-hover:text-primary group-hover:opacity-100" />
                  </div>
                  <p className="mt-1 truncate text-xs font-medium text-primary">
                    {portfolio.url}
                  </p>
                  {portfolio.description ? (
                    <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
                      {portfolio.description}
                    </p>
                  ) : null}
                </div>
              </a>
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">No portfolio links yet.</p>
        )}
      </PanelBody>
    </Panel>
  );
}
