export interface FileMetadataDto {
  id: string;
  originalFileName: string;
  contentType: string;
  sizeInBytes: number;
  isDeleted: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface FileListResponse {
  items: FileMetadataDto[];
  total: number;
  page: number;
  pageSize: number;
}

export interface FileDownloadUrlResponse {
  fileId: string;
  url: string;
  expiresAt: string;
}
