// Generic API response types matching backend

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

export interface ErrorDetail {
  code: string;
  message: string;
  field?: string;
}

export interface ErrorResponse {
  success: boolean;
  message: string;
  errors: ErrorDetail[];
}

// For future pagination support
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
