"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Download, FileText, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { EmptyState } from "@/components/workspace/empty-state";
import { PageHeader } from "@/components/workspace/page-header";
import { getApiErrorMessage } from "@/lib/api";
import { queryKeys } from "@/lib/query-keys";
import { fileService } from "@/services";

export default function FilesPage() {
  const queryClient = useQueryClient();
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const filesQuery = useQuery({ queryKey: queryKeys.files, queryFn: () => fileService.listMine(1, 100) });
  const deleteMutation = useMutation({
    mutationFn: fileService.delete,
    onSuccess: () => {
      toast.success("File deleted.");
      setDeleteId(null);
      void queryClient.invalidateQueries({ queryKey: queryKeys.files });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  async function download(fileId: string) {
    try {
      const result = await fileService.getDownloadUrl(fileId);
      window.open(result.url, "_blank", "noopener,noreferrer");
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    }
  }

  if (filesQuery.isLoading) return <LoadingState label="Loading files" />;
  const files = filesQuery.data?.items ?? [];

  return (
    <div className="space-y-5">
      <PageHeader title="Files" description="Manage uploaded CVs, pitch decks, and portfolio attachments owned by your account." />
      {filesQuery.error ? <p className="rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">{getApiErrorMessage(filesQuery.error)}</p> : null}
      {files.length === 0 ? <EmptyState icon={FileText} title="No files" description="Uploaded files will appear here." /> : (
        <Panel>
          <PanelHeader><PanelTitle>{filesQuery.data?.total ?? files.length} files</PanelTitle></PanelHeader>
          <PanelBody className="overflow-x-auto">
            <table className="w-full min-w-[720px] text-left text-sm">
              <thead className="border-b border-border text-xs text-muted-foreground"><tr><th className="py-2">Name</th><th>Type</th><th>Size</th><th>Created</th><th className="text-right">Actions</th></tr></thead>
              <tbody>{files.map((file) => (
                <tr key={file.id} className="border-b border-border">
                  <td className="py-3 font-medium">{file.originalFileName}</td><td>{file.contentType}</td><td>{formatBytes(file.sizeInBytes)}</td><td>{new Date(file.createdAt).toLocaleDateString()}</td>
                  <td><div className="flex justify-end gap-2"><Button size="icon" variant="outline" aria-label={`Download ${file.originalFileName}`} onClick={() => void download(file.id)}><Download className="h-4 w-4" /></Button><Button size="icon" variant="danger" aria-label={`Delete ${file.originalFileName}`} onClick={() => setDeleteId(file.id)}><Trash2 className="h-4 w-4" /></Button></div></td>
                </tr>
              ))}</tbody>
            </table>
          </PanelBody>
        </Panel>
      )}
      <ConfirmDialog open={Boolean(deleteId)} title="Delete file?" description="This removes the stored file and cannot be undone." confirmLabel="Delete file" isLoading={deleteMutation.isPending} onClose={() => setDeleteId(null)} onConfirm={() => deleteId && deleteMutation.mutate(deleteId)} />
    </div>
  );
}

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
