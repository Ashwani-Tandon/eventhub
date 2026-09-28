// This helper turns HTTP failures into readable feedback across feature screens.
// It preserves business and validation messages without exposing request credentials.
import { HttpErrorResponse } from '@angular/common/http';
// Prefer meaningful field errors, then the API's business detail.
export function apiError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return 'Something went wrong. Please try again.';
  if (error.status === 0 || error.status === 503)
    return 'Service temporarily unavailable. Please try again.';
  if (error.status === 401) return 'Please sign in again.';
  if (error.status === 403) return 'You do not have permission to do that.';
  if (error.status === 422)
    return 'Payment failed. No booking was created; reserved seats have been returned.';
  const body: unknown = error.error;
  if (body && typeof body === 'object') {
    if ('errors' in body && body.errors && typeof body.errors === 'object') {
      const messages = Object.values(body.errors)
        .flat()
        .filter((v): v is string => typeof v === 'string');
      if (messages.length) return messages.join(' ');
    }
    if ('detail' in body && typeof body.detail === 'string') return body.detail;
  }
  return `Request failed (${error.status}). Please try again.`;
}
