// The shell presents navigation appropriate to the current signed-in role.
// Feature routes share this layout while their screens load independently.
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../core/auth/auth.service';
import { Role, ROLES } from '../../core/models/auth.models';
interface MenuItem {
  label: string;
  path: string;
  roles?: readonly Role[];
}
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, MatButtonModule],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly items: readonly MenuItem[] = [
    { label: 'Events', path: '/events' },
    { label: 'My Bookings', path: '/my-bookings', roles: Object.values(ROLES) },
    { label: 'My Events', path: '/my-events', roles: [ROLES.organizer, ROLES.admin] },
    { label: 'Dashboard', path: '/dashboard', roles: [ROLES.organizer, ROLES.admin] },
    { label: 'Users', path: '/admin/users', roles: [ROLES.admin] },
    { label: 'All Bookings', path: '/admin/bookings', roles: [ROLES.admin] },
  ];
  readonly menu = computed(() => {
    const role = this.auth.role();
    return this.items.filter((item) => !item.roles || (role && item.roles.includes(role)));
  });
  // Refresh needs server signature validation, beyond locally decoded display claims.
  constructor() {
    this.auth.verifyRestoredSession();
  }
  // The explicit logout action returns to the public starting page.
  logout() {
    this.auth.logout();
    void this.router.navigate(['/events']);
  }
}
