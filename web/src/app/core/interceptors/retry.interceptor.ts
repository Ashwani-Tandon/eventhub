// Retries temporary failures only while reading EventHub data.
// Writes stay under the user's control so a lost response never repeats an action automatically.
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { retry, throwError, timer } from 'rxjs';
const RETRY_DELAYS = [500, 1000];
const TRANSIENT_STATUSES = [0, 502, 503, 504];
// RxJS resubscribes to the same GET after a short pause; cancelling navigation cancels the pause too.
export const retryInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.method !== 'GET' || !request.url.startsWith('/api/')) return next(request);
  return next(request).pipe(
    retry({
      count: RETRY_DELAYS.length,
      // Wait half a second, then one second, to give a restarting service time to recover.
      delay: (error: unknown, retryCount) =>
        error instanceof HttpErrorResponse && TRANSIENT_STATUSES.includes(error.status)
          ? timer(RETRY_DELAYS[retryCount - 1])
          : throwError(() => error),
    }),
  );
};
