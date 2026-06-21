"use client";

import type { ReactNode } from "react";
import { AlertTriangle } from "lucide-react";
import { Button } from "./button";

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel = "Confirm",
  isLoading = false,
  onConfirm,
  onClose,
  children,
}: {
  open: boolean;
  title: string;
  description: string;
  confirmLabel?: string;
  isLoading?: boolean;
  onConfirm: () => void;
  onClose: () => void;
  children?: ReactNode;
}) {
  if (!open) return null;

  return (
    <div className="fixed inset-0 z-[70] flex items-center justify-center p-4" role="dialog" aria-modal="true" aria-labelledby="confirm-title">
      <button type="button" className="absolute inset-0 bg-black/45" aria-label="Close confirmation" onClick={onClose} />
      <div className="relative w-full max-w-md rounded-md border border-border bg-card p-5 shadow-xl">
        <div className="flex items-start gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-destructive/10 text-destructive"><AlertTriangle className="h-5 w-5" /></span>
          <div>
            <h2 id="confirm-title" className="font-semibold">{title}</h2>
            <p className="mt-1 text-sm text-muted-foreground">{description}</p>
          </div>
        </div>
        {children ? <div className="mt-4">{children}</div> : null}
        <div className="mt-5 flex justify-end gap-2">
          <Button variant="outline" onClick={onClose} disabled={isLoading}>Cancel</Button>
          <Button variant="danger" onClick={onConfirm} disabled={isLoading}>{isLoading ? "Working..." : confirmLabel}</Button>
        </div>
      </div>
    </div>
  );
}
