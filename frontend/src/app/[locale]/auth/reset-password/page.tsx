"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { KeyRound, Loader2 } from "lucide-react";
import { Suspense, useState } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { AuthMessage } from "@/components/auth/auth-message";
import { FormField } from "@/components/auth/form-field";
import { AuthShell } from "@/components/auth/auth-shell";
import { getApiErrorMessage } from "@/lib/api";
import { resetPasswordSchema, type ResetPasswordFormValues } from "@/lib/validations/auth";
import { backendService } from "@/services";

export default function ResetPasswordPage() {
  return (
    <Suspense fallback={null}>
      <ResetPasswordForm />
    </Suspense>
  );
}

function ResetPasswordForm() {
  const searchParams = useSearchParams();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      email: searchParams.get("email") ?? "",
      token: searchParams.get("token") ?? "",
      newPassword: "",
      confirmPassword: "",
    },
  });

  async function onSubmit(values: ResetPasswordFormValues) {
    setError(null);
    setSuccess(false);
    try {
      await backendService.resetPassword({
        email: values.email,
        token: values.token,
        newPassword: values.newPassword,
      });
      setSuccess(true);
    } catch (submitError) {
      setError(getApiErrorMessage(submitError));
    }
  }

  return (
    <AuthShell
      title="Choose new password"
      description="Paste your reset token and set a new password for your account."
      footer={
        <>
          Ready to continue?{" "}
          <Link className="font-medium text-primary hover:underline" href="/auth/login">
            Sign in
          </Link>
        </>
      }
    >
      <form className="space-y-4" onSubmit={handleSubmit(onSubmit)}>
        {error ? <AuthMessage tone="error">{error}</AuthMessage> : null}
        {success ? <AuthMessage tone="success">Your password has been reset. You can sign in now.</AuthMessage> : null}
        <FormField label="Email" type="email" autoComplete="email" error={errors.email} registration={register("email")} />
        <FormField label="Reset token" autoComplete="one-time-code" error={errors.token} registration={register("token")} />
        <FormField label="New password" type="password" autoComplete="new-password" error={errors.newPassword} registration={register("newPassword")} />
        <FormField label="Confirm new password" type="password" autoComplete="new-password" error={errors.confirmPassword} registration={register("confirmPassword")} />
        <Button className="w-full" type="submit" disabled={isSubmitting}>
          {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <KeyRound className="h-4 w-4" />}
          Reset password
        </Button>
      </form>
    </AuthShell>
  );
}
