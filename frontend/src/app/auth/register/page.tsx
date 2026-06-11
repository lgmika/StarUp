"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, UserPlus } from "lucide-react";
import { Suspense, useState } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { AuthMessage } from "@/components/auth/auth-message";
import { FormField } from "@/components/auth/form-field";
import { AuthShell } from "@/components/auth/auth-shell";
import { getApiErrorMessage } from "@/lib/api";
import { registerSchema, type RegisterFormValues } from "@/lib/validations/auth";
import { useAuthStore } from "@/stores/auth-store";

function getRedirectTarget(next: string | null) {
  if (next?.startsWith("/") && !next.startsWith("//")) return next;
  return "/";
}

export default function RegisterPage() {
  return (
    <Suspense fallback={null}>
      <RegisterForm />
    </Suspense>
  );
}

function RegisterForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const registerAccount = useAuthStore((state) => state.register);
  const [error, setError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { fullName: "", email: "", password: "", confirmPassword: "" },
  });

  async function onSubmit(values: RegisterFormValues) {
    setError(null);
    try {
      await registerAccount(values);
      router.replace(getRedirectTarget(searchParams.get("next")));
      router.refresh();
    } catch (submitError) {
      setError(getApiErrorMessage(submitError));
    }
  }

  return (
    <AuthShell
      title="Create account"
      description="Start with your name, email, and a password that satisfies backend validation."
      footer={
        <>
          Already have an account?{" "}
          <Link className="font-medium text-primary hover:underline" href="/auth/login">
            Sign in
          </Link>
        </>
      }
    >
      <form className="space-y-4" onSubmit={handleSubmit(onSubmit)}>
        {error ? <AuthMessage tone="error">{error}</AuthMessage> : null}
        <FormField label="Full name" autoComplete="name" error={errors.fullName} registration={register("fullName")} />
        <FormField label="Email" type="email" autoComplete="email" error={errors.email} registration={register("email")} />
        <FormField label="Password" type="password" autoComplete="new-password" error={errors.password} registration={register("password")} />
        <FormField label="Confirm password" type="password" autoComplete="new-password" error={errors.confirmPassword} registration={register("confirmPassword")} />
        <Button className="w-full" type="submit" disabled={isSubmitting}>
          {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <UserPlus className="h-4 w-4" />}
          Create account
        </Button>
      </form>
    </AuthShell>
  );
}
