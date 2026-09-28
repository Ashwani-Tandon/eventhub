// This page displays one Catalog event and opens the purchase dialog.
// A successful purchase reloads seat availability instead of guessing the new count.
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { finalize } from 'rxjs';
import { CatalogApiService } from '../../core/api/catalog-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { EventItem } from '../../core/models/event.models';
import { BookingDialog } from '../bookings/booking-dialog';
import { apiError } from '../../shared/api-error';
@Component({
  selector: 'app-event-details',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, RouterLink, MatButtonModule],
  templateUrl: './event-details.html',
})
export class EventDetails {
  private readonly api = inject(CatalogApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  readonly event = signal<EventItem | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  // Load the route-selected event even for a guest or historical date.
  constructor() {
    this.load();
  }
  // Fetch current seats and show a readable missing-event or service error.
  load() {
    this.loading.set(true);
    this.error.set('');
    this.api
      .get(Number(this.route.snapshot.paramMap.get('id')))
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (event) => this.event.set(event),
        error: (error) => {
          this.event.set(null);
          this.error.set(apiError(error));
        },
      });
  }
  // Historical events stay readable but cannot start a new booking.
  started() {
    return new Date(this.event()?.startsAt ?? '').getTime() <= Date.now();
  }
  // Guests sign in and return here; logged-in users review quantity in a modal.
  book() {
    const event = this.event();
    if (!event) return;
    if (!this.auth.currentUser()) {
      void this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    this.dialog
      .open(BookingDialog, { data: event, width: '480px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((result) => {
        if (result) this.load();
      });
  }
}
