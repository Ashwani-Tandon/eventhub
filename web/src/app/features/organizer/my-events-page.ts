// Managed events are scoped by Catalog to the organizer or all events for Admin.
// Delete asks for confirmation and displays Catalog's booked-seat conflict message.
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { CatalogApiService } from '../../core/api/catalog-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { EventItem } from '../../core/models/event.models';
import { ROLES } from '../../core/models/auth.models';
import { ConfirmDialog } from '../../shared/confirm-dialog';
import { apiError } from '../../shared/api-error';
@Component({
  selector: 'app-my-events-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, RouterLink, MatButtonModule],
  templateUrl: './my-events-page.html',
})
export class MyEventsPage {
  private readonly api = inject(CatalogApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackbar = inject(MatSnackBar);
  readonly auth = inject(AuthService);
  readonly adminRole = ROLES.admin;
  readonly events = signal<EventItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly deleting = signal<number | null>(null);
  // Fetch the managed list when the user arrives from their role menu.
  constructor() {
    this.load();
  }
  // Refresh after create/edit navigation or deletion using authoritative server data.
  load() {
    this.loading.set(true);
    this.error.set('');
    this.api
      .mine()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (events) => this.events.set(events),
        error: (error) => this.error.set(apiError(error)),
      });
  }
  // Keep the event unless the user confirms the named destructive action.
  delete(event: EventItem) {
    this.dialog
      .open(ConfirmDialog, {
        data: {
          title: 'Delete event?',
          message: `Delete ${event.title}? This cannot be undone. An event with booked seats cannot be deleted.`,
          action: 'Delete event',
        },
      })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed) this.performDelete(event.id);
      });
  }
  // A 409 is shown as the server's business message rather than pretending deletion worked.
  private performDelete(id: number) {
    if (this.deleting() !== null) return;
    this.deleting.set(id);
    this.api
      .delete(id)
      .pipe(finalize(() => this.deleting.set(null)))
      .subscribe({
        next: () => {
          this.snackbar.open('Event deleted.', 'Close', { duration: 4000 });
          this.load();
        },
        error: (error) => this.snackbar.open(apiError(error), 'Close', { duration: 7000 }),
      });
  }
}
