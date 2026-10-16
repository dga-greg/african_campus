import { HttpErrorResponse } from '@angular/common/http';
import type { ApiErrorBody, ApiErrorDetail, ErrorCode } from '@fh/contracts';

/** A normalised API failure that UI code can display without knowing about HTTP. */
export class ApiError extends Error {
  constructor(
    readonly code: ErrorCode | 'NETWORK',
    message: string,
    readonly status: number,
    readonly details: ApiErrorDetail[] = [],
    readonly requestId?: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  fieldError(path: string): string | undefined {
    return this.details.find((d) => d.path === path)?.message;
  }

  static from(err: unknown): ApiError {
    if (err instanceof ApiError) return err;
    if (err instanceof HttpErrorResponse) {
      const body = err.error as Partial<ApiErrorBody> | null;
      if (body?.error?.code) {
        return new ApiError(body.error.code, body.error.message, err.status, body.error.details ?? [], body.error.requestId);
      }
      if (err.status === 0) {
        return new ApiError('NETWORK', 'Cannot reach the server. Check your connection and try again.', 0);
      }
      return new ApiError('INTERNAL', 'Something went wrong. Please try again later.', err.status);
    }
    return new ApiError('INTERNAL', 'Something went wrong. Please try again later.', 0);
  }
}
