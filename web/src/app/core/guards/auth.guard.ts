// These guards explain browser navigation rules before a page is displayed.
// Backend authorization remains the security boundary even if a guard is bypassed.
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { Role } from '../models/auth.models';
// Guests must sign in, then return to the protected local destination.
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  return auth.currentUser()
    ? true
    : inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};
// A signed-in user with the wrong role returns to the public Events page.
export function roleGuard(roles: readonly Role[]): CanActivateFn {
  return () => {
    const role = inject(AuthService).role();
    return role && roles.includes(role) ? true : inject(Router).createUrlTree(['/events']);
  };
}
