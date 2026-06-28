"use client";

import Link from "next/link";
import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, Mail } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { AuthMessage } from "@/components/auth/auth-message";
import { FormField } from "@/components/auth/form-field";
import { AuthShell } from "@/components/auth/auth-shell";
import { getApiErrorMessage } from "@/lib/api";
import { forgotPasswordSchema, type ForgotPasswordFormValues } from "@/lib/validations/auth";
import { backendService } from "@/services";

export default function ForgotPasswordPage() {
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [devToken, setDevToken] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: "" },
  });

  async function onSubmit(values: ForgotPasswordFormValues) {
    setError(null);
    setMessage(null);
    setDevToken(null);
    try {
      const response = await backendService.forgotPassword(values);
      setMessage(response.message);
      setDevToken(response.devPasswordResetToken ?? null);
    } catch (submitError) {
      setError(getApiErrorMessage(submitError));
    }
  }

  return (
    <AuthShell
      title="Reset password"
      description="Enter your account email and use the reset token to choose a new password."
      footer={
        <>
          Remembered it?{" "}
          <Link className="font-medium text-primary hover:underline" href="/auth/login">
            Back to sign in
          </Link>
        </>
      }
    >
      <form className="space-y-4" onSubmit={handleSubmit(onSubmit)}>
        {error ? <AuthMessage tone="error">{error}</AuthMessage> : null}
        {message ? (
          <AuthMessage tone="success">
            <p>{message}</p>
            {devToken ? (
              <p className="mt-2 break-all text-xs">
                Development token: <span className="font-mono">{devToken}</span>
              </p>
            ) : null}
          </AuthMessage>
        ) : null}
        <FormField label="Email" type="email" autoComplete="email" error={errors.email} registration={register("email")} />
        <Button className="w-full" type="submit" disabled={isSubmitting}>
          {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Mail className="h-4 w-4" />}
          Send reset token
        </Button>
        <Link
          className="inline-flex h-10 w-full items-center justify-center rounded-md border border-border bg-background px-4 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground"
          href="/auth/reset-password"
        >
          I have a reset token
        </Link>
      </form>
    </AuthShell>
  );
}
