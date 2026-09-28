// This file wires routing and the HTTP authentication and read-retry interceptors at startup.
// Functional providers keep the standalone application free of module scaffolding.
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { retryInterceptor } from './core/interceptors/retry.interceptor';
import { authInterceptor } from './core/interceptors/auth.interceptor';
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, retryInterceptor])),
  ],
};
