// This page shows the caller's booking snapshots and repeatable cancellation.
// Pending seat returns remain visible so users can finish cancellation after recovery.
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { BookingApiService } from '../../core/api/booking-api.service';
import { Booking } from '../../core/models/booking.models';
import { ConfirmDialog } from '../../shared/confirm-dialog';
import { apiError } from '../../shared/api-error';
@Component({
  selector: 'app-bookings-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, RouterLink, MatButtonModule],
  templateUrl: './bookings-page.html',
})
export class BookingsPage {
  private readonly api = inject(BookingApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackbar = inject(MatSnackBar);
  readonly bookings = signal<Booking[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly cancelling = signal<number | null>(null);
  // Read only Booking; the history screen does not depend on Catalog.
  constructor() {
    this.load();
  }
  // Refresh after mutations so pending release flags match durable server state.
  load() {
    this.loading.set(true);
    this.error.set('');
    this.api
      .mine()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (bookings) => this.bookings.set(bookings),
        error: (error) => this.error.set(apiError(error)),
      });
  }
  // An already accepted cancellation may finish its release even after the event starts.
  canCancel(booking: Booking) {
    return (
      booking.seatReleasePending ||
      (booking.status === 'Confirmed' && new Date(booking.eventStartsAt).getTime() > Date.now())
    );
  }
  // Confirm before beginning cancellation; a pending release is labelled as a retry.
  cancel(booking: Booking) {
    this.dialog
      .open(ConfirmDialog, {
        data: {
          title: booking.seatReleasePending ? 'Finish cancellation?' : 'Cancel booking?',
          message: `${booking.quantity} ticket(s) for ${booking.eventTitle}. Seats will be returned when cancellation finishes.`,
          action: booking.seatReleasePending ? 'Retry seat return' : 'Cancel booking',
        },
      })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed) this.performCancel(booking.id);
      });
  }
  // Reload even on 503 because cancellation may already be saved with release pending.
  private performCancel(id: number) {
    if (this.cancelling() !== null) return;
    this.cancelling.set(id);
    this.api
      .cancel(id)
      .pipe(finalize(() => this.cancelling.set(null)))
      .subscribe({
        next: () => {
          this.snackbar.open('Booking cancelled. Seats returned.', 'Close', { duration: 5000 });
          this.load();
        },
        error: (error) => {
          this.snackbar.open(apiError(error), 'Close', { duration: 7000 });
          this.load();
        },
      });
  }
}
