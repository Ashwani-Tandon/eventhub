// This interceptor attaches the session only to EventHub's relative API calls.
// A rejected token clears identity and returns the user to login without retrying.
import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const isApi = request.url.startsWith('/api/');
  const token = auth.token();
  const outgoing =
    isApi && token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
  return next(outgoing).pipe(
    catchError((error: unknown) => {
      if (isApi && error instanceof HttpErrorResponse && error.status === 401) auth.endSession();
      return throwError(() => error);
    }),
  );
};
