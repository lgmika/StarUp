"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { CheckCircle2, Loader2 } from "lucide-react";
import { Suspense, useState } from "react";
import { useForm } from "react-hook-form";
import { AuthMessage } from "@/components/auth/auth-message";
import { AuthShell } from "@/components/auth/auth-shell";
import { FormField } from "@/components/auth/form-field";
import { Button } from "@/components/ui/button";
import { getApiErrorMessage } from "@/lib/api";
import { verifyEmailSchema, type VerifyEmailFormValues } from "@/lib/validations/auth";
import { backendService } from "@/services";

export default function VerifyEmailPage() {
  return (
    <Suspense fallback={null}>
      <VerifyEmailForm />
    </Suspense>
  );
}

function VerifyEmailForm() {
  const searchParams = useSearchParams();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<VerifyEmailFormValues>({
    resolver: zodResolver(verifyEmailSchema),
    defaultValues: {
      email: searchParams.get("email") ?? "",
      token: searchParams.get("token") ?? "",
    },
  });

  async function onSubmit(values: VerifyEmailFormValues) {
    setError(null);
    setSuccess(false);
    try {
      await backendService.verifyEmail(values);
      setSuccess(true);
    } catch (submitError) {
      setError(getApiErrorMessage(submitError));
    }
  }

  return (
    <AuthShell
      title="Verify email"
      description="Confirm your account email with the verification token from StartupConnect."
      footer={
        <>
          Already verified?{" "}
          <Link className="font-medium text-primary hover:underline" href="/auth/login">
            Sign in
          </Link>
        </>
      }
    >
      <form className="space-y-4" onSubmit={handleSubmit(onSubmit)}>
        {error ? <AuthMessage tone="error">{error}</AuthMessage> : null}
        {success ? <AuthMessage tone="success">Your email has been verified. You can continue to sign in.</AuthMessage> : null}
        <FormField label="Email" type="email" autoComplete="email" error={errors.email} registration={register("email")} />
        <FormField label="Verification token" autoComplete="one-time-code" error={errors.token} registration={register("token")} />
        <Button className="w-full" type="submit" disabled={isSubmitting || success}>
          {isSubmitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <CheckCircle2 className="h-4 w-4" />}
          Verify email
        </Button>
      </form>
    </AuthShell>
  );
}
