// This page lets Admin inspect safe profiles and save one user's selected role.
// It does not rewrite active tokens, so the affected user must sign in again.
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { IdentityApiService } from '../../core/api/identity-api.service';
import { Role, ROLES, User } from '../../core/models/auth.models';
import { apiError } from '../../shared/api-error';
interface UserRow {
  user: User;
  role: FormControl<Role>;
}
@Component({
  selector: 'app-users-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, MatButtonModule],
  templateUrl: './users-page.html',
})
export class UsersPage {
  private readonly api = inject(IdentityApiService);
  private readonly snackbar = inject(MatSnackBar);
  readonly rows = signal<UserRow[]>([]);
  readonly roles = Object.values(ROLES);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly saving = signal<string | null>(null);
  // Read current stored roles; they can differ from claims in existing sessions.
  constructor() {
    this.load();
  }
  // Create a typed selector for each returned user without modifying their profile early.
  load() {
    this.loading.set(true);
    this.error.set('');
    this.api
      .users()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (users) =>
          this.rows.set(
            users.map((user) => ({
              user,
              role: new FormControl(user.role, { nonNullable: true }),
            })),
          ),
        error: (error) => this.error.set(apiError(error)),
      });
  }
  // Save only the selected row; menus change on that user's next login.
  save(row: UserRow) {
    if (this.saving()) return;
    this.saving.set(row.user.id);
    this.api
      .changeRole(row.user.id, row.role.value)
      .pipe(finalize(() => this.saving.set(null)))
      .subscribe({
        next: (user) => {
          this.rows.update((rows) =>
            rows.map((item) =>
              item.user.id === user.id
                ? { user, role: new FormControl(user.role, { nonNullable: true }) }
                : item,
            ),
          );
          this.snackbar.open('Role saved. Applies at the user’s next login.', 'Close', {
            duration: 6000,
          });
        },
        error: (error) => this.snackbar.open(apiError(error), 'Close', { duration: 7000 }),
      });
  }
}
