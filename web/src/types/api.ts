/**
 * Shared API response shapes — kept in sync with `backend/src/Rmms.Shared`.
 * Per knowledge-base/05-api-conventions.md.
 */

export type ApiResponse<T> = {
  data: T;
  meta?: Record<string, unknown>;
};

export type PaginatedResponse<T> = {
  data: T[];
  meta: PaginationMeta;
};

export type PaginationMeta = {
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
};

export type ApiError = {
  error: {
    code: string;
    message: string;
    details?: ApiErrorDetail[];
    traceId?: string;
  };
};

export type ApiErrorDetail = {
  field: string;
  code: string;
  message: string;
};

export type UserRole = "pg" | "leader" | "buh" | "admin";
