"use client";

import { useState } from "react";
import { Loader2, Upload } from "lucide-react";
import { toast } from "sonner";
import { useQueryClient } from "@tanstack/react-query";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { MAX_CV_UPLOAD_BYTES } from "@/lib/config";
import { profileService } from "@/services";

export function CvUpload() {
  const queryClient = useQueryClient();
  const [isUploading, setIsUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);

  async function uploadCv(file: File | undefined) {
    if (!file) return;
    if (file.type !== "application/pdf") {
      toast.error("Only PDF uploads are supported.");
      return;
    }
    if (file.size > MAX_CV_UPLOAD_BYTES) {
      toast.error(`PDF must be smaller than ${formatBytes(MAX_CV_UPLOAD_BYTES)}.`);
      return;
    }

    setIsUploading(true);
    setUploadProgress(0);
    try {
      await profileService.uploadCv(file, setUploadProgress);
      toast.success("PDF uploaded.");
      void queryClient.invalidateQueries({ queryKey: ["cvs"] });
    } catch (submitError) {
      toast.error(getApiErrorMessage(submitError));
    } finally {
      setIsUploading(false);
      setUploadProgress(0);
    }
  }

  return (
    <Panel>
      <PanelHeader>
        <PanelTitle>Upload PDF</PanelTitle>
      </PanelHeader>
      <PanelBody>
        <label className="group flex cursor-pointer flex-col items-center justify-center rounded-xl border-2 border-dashed border-border p-8 text-center transition-all hover:border-primary hover:bg-primary/5">
          {isUploading ? (
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
              <Loader2 className="h-6 w-6 animate-spin" />
            </div>
          ) : (
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-muted text-muted-foreground transition-colors group-hover:bg-primary/10 group-hover:text-primary">
              <Upload className="h-6 w-6" />
            </div>
          )}
          <span className="mt-4 text-sm font-semibold">Choose PDF file</span>
          <span className="mt-1 text-xs text-muted-foreground">
            PDF only, up to {formatBytes(MAX_CV_UPLOAD_BYTES)}.
          </span>
          {isUploading ? (
            <div
              className="mt-4 w-full max-w-[200px]"
              role="progressbar"
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={uploadProgress}
            >
              <div className="h-2 overflow-hidden rounded-full bg-muted">
                <div
                  className="h-full bg-primary transition-all duration-300 ease-out"
                  style={{ width: `${uploadProgress}%` }}
                />
              </div>
              <span className="mt-2 block text-xs font-medium text-muted-foreground">
                {uploadProgress}% uploaded
              </span>
            </div>
          ) : null}
          <input
            aria-label="Upload CV PDF"
            className="sr-only"
            type="file"
            accept="application/pdf"
            disabled={isUploading}
            onChange={(event) => {
              void uploadCv(event.target.files?.[0]);
              // Reset the input so the same file can be selected again if needed
              event.target.value = "";
            }}
          />
        </label>
      </PanelBody>
    </Panel>
  );
}

function formatBytes(bytes: number) {
  return `${Math.round(bytes / 1024 / 1024)} MB`;
}
