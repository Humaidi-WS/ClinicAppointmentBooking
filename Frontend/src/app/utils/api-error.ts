import { HttpErrorResponse } from '@angular/common/http';

export function extractApiError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Something went wrong. Please try again.';
  }

  const body = error.error as Record<string, unknown> | string | null;

  if (typeof body === 'string' && body.trim()) {
    return body;
  }

  if (body && typeof body === 'object') {
    const direct =
      (typeof body['error'] === 'string' && body['error']) ||
      (typeof body['Error'] === 'string' && body['Error']) ||
      (typeof body['detail'] === 'string' && body['detail']) ||
      (typeof body['title'] === 'string' && body['title']);

    if (direct) {
      return direct;
    }

    const validation = body['errors'];
    if (validation && typeof validation === 'object') {
      const messages = Object.values(validation as Record<string, unknown>)
        .flatMap((value) => (Array.isArray(value) ? value : [value]))
        .filter((value): value is string => typeof value === 'string');

      if (messages.length) {
        return messages.join(' ');
      }
    }
  }

  if (error.status === 0) {
    return 'Cannot reach the clinic API. Start the backend on http://localhost:5246.';
  }

  return error.statusText || 'Request failed.';
}
