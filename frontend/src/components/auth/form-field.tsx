import type { InputHTMLAttributes } from "react";
import type { FieldError, UseFormRegisterReturn } from "react-hook-form";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

interface FormFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: FieldError;
  registration: UseFormRegisterReturn;
}

export function FormField({ label, error, registration, className, ...props }: FormFieldProps) {
  return (
    <label className="block space-y-1.5 text-sm font-medium">
      <span>{label}</span>
      <Input
        aria-invalid={!!error}
        className={cn(error && "border-destructive focus-visible:ring-destructive", className)}
        {...registration}
        {...props}
      />
      {error ? <span className="block text-xs font-normal text-destructive">{error.message}</span> : null}
    </label>
  );
}
