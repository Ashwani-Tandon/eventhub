// AuthService owns the browser session and exposes signals to menus and guards.
// JWT decoding restores display claims; only the backend verifies their signature.
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { jwtDecode, JwtPayload } from 'jwt-decode';
import { tap } from 'rxjs';
import { IdentityApiService } from '../api/identity-api.service';
import {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  ROLES,
  TOKEN_KEY,
  User,
} from '../models/auth.models';
interface Claims extends JwtPayload {
  email: string;
  name: string;
  role: User['role'];
}
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(IdentityApiService);
  private readonly router = inject(Router);
  private readonly userState = signal<User | null>(null);
  readonly currentUser = this.userState.asReadonly();
  readonly role = computed(() => this.currentUser()?.role ?? null);
  private expiryTimer?: ReturnType<typeof setTimeout>;
  // Restore claims immediately so guards work on a direct URL after refresh.
  constructor() {
    this.restore();
  }
  // Read storage for each API call, including when the owner replaces a token manually.
  token() {
    return localStorage.getItem(TOKEN_KEY);
  }
  // Save the API's session before subscribers navigate to their destination.
  login(body: LoginRequest) {
    return this.api.login(body).pipe(tap((response) => this.accept(response)));
  }
  // Registration returns the same session contract as login.
  register(body: RegisterRequest) {
    return this.api.register(body).pipe(tap((response) => this.accept(response)));
  }
  // Remove both persisted and visible identity so menus cannot retain the old role.
  logout() {
    if (this.expiryTimer) clearTimeout(this.expiryTimer);
    this.expiryTimer = undefined;
    localStorage.removeItem(TOKEN_KEY);
    this.userState.set(null);
  }
  // Used by 401 handling and expiry; login retains only a local protected return URL.
  endSession(returnUrl = this.router.url) {
    this.logout();
    // A failed sign-in should keep its form and original destination visible.
    if (/^\/login([/?#]|$)/.test(this.router.url)) return;
    void this.router.navigate(['/login'], {
      queryParams: { returnUrl: this.safeReturnUrl(returnUrl) },
    });
  }
  // Reject external and auth-page destinations before navigating after sign-in.
  safeReturnUrl(value: string | null) {
    return value &&
      value.startsWith('/') &&
      !value.startsWith('//') &&
      !value.includes('\\') &&
      !/^\/(login|register)([/?#]|$)/.test(value)
      ? value
      : '/events';
  }
  // Ask the backend to verify a restored token; a network outage does not mean logout.
  verifyRestoredSession() {
    if (!this.token()) return;
    const restoredToken = this.token();
    this.api.me().subscribe({
      // Ignore an old profile response if the user signed out while it was loading.
      next: (user) => {
        if (this.token() === restoredToken && this.currentUser()) this.userState.set(user);
      },
      error: () => undefined,
    });
  }
  // Only accept a usable JWT and schedule sign-out using its expiry claim.
  private accept(response: LoginResponse) {
    localStorage.setItem(TOKEN_KEY, response.accessToken);
    this.restore();
  }
  // Malformed, missing-claim, and expired tokens cannot create a visible session.
  private restore() {
    const token = this.token();
    if (!token) return;
    try {
      const claims = jwtDecode<Claims>(token);
      if (
        !claims.exp ||
        claims.exp * 1000 <= Date.now() ||
        !claims.sub ||
        !claims.email ||
        !claims.name ||
        !Object.values(ROLES).includes(claims.role)
      ) {
        this.logout();
        return;
      }
      this.userState.set({
        id: claims.sub,
        email: claims.email,
        fullName: claims.name,
        role: claims.role,
      });
      if (this.expiryTimer) clearTimeout(this.expiryTimer);
      // The two-hour JWT fits the browser timer limit; expiry sends the user to login.
      this.expiryTimer = setTimeout(() => this.endSession(), claims.exp * 1000 - Date.now());
    } catch {
      // Keep an unreadable token only until /auth/me rejects it with 401.
      // It never supplies a visible identity; the interceptor then removes it.
      this.userState.set(null);
    }
  }
}
