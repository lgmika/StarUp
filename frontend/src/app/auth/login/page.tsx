"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, LogIn } from "lucide-react";
import { Suspense, useState } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { AuthMessage } from "@/components/auth/auth-message";
import { FormField } from "@/components/auth/form-field";
import { AuthShell } from "@/components/auth/auth-shell";
import { getApiErrorMessage } from "@/lib/api";
import { loginSchema, type LoginFormValues } from "@/lib/validations/auth";
import { useAuthStore } from "@/stores/auth-store";

function getRedirectTarget(next: string | null) {
  if (next?.startsWith("/") && !next.startsWith("//")) return next;
  return "/";
}

export default function LoginPage() {
  return (
    <Suspense fallback={null}>
      <LoginForm />
    </Suspense>
  );
}

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const login = useAuthStore((state) => state.login);
  const [error, setError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  async function onSubmit(values: LoginFormValues) {
    setError(null);
    try {
      await login(values);
      router.replace(getRedirectTarget(searchParams.get("next")));
      router.refresh();
    } catch (submitError) {
      setError(getApiErrorMessage(submitError));
    }
  }

  return (
    <AuthShell
      title="Sign in"
      description="Use your StartupConnect account to continue to the console."
      footer={
        <>
          New to StartupConnect?{" "}
          <Link className="font-medium text-primary hover:underline" href="/auth/register">
            Create an account
          </Link>
        </>
      }
    >
      <form className="space-y-4" onSubmit={handleSubmit(onSubmit)}>
        {error ? <AuthMessage tone="error">{error}</AuthMessage> : null}
        <FormField label="Email" type="email" autoComplete="email" error={errors.email} registration={register("email")} />
        <FormField label="Password" type="password" autoComplete="current-password" error={errors.password} registration={register("password")} />
        <div className="flex justify-end">
          <Link className="text-sm font-medium text-primary hover:underline" href="/auth/forgot-password">
            Forgot password?
          </Link>
        </div>
        <Button className="w-full" type="submit" disabled={isSubmitting}>
          {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <LogIn className="h-4 w-4" />}
          Sign in
        </Button>
      </form>
    </AuthShell>
  );
}
