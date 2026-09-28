// Admin sees every Booking snapshot and may narrow the list by status.
// No cancellation action is exposed here because Admin cannot cancel another user's booking.
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Subscription } from 'rxjs';
import { BookingApiService } from '../../core/api/booking-api.service';
import { Booking, BookingStatus } from '../../core/models/booking.models';
import { apiError } from '../../shared/api-error';
@Component({
  selector: 'app-all-bookings-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule, RouterLink, MatButtonModule],
  templateUrl: './all-bookings-page.html',
})
export class AllBookingsPage {
  private readonly api = inject(BookingApiService);
  private request?: Subscription;
  private readonly destroyRef = inject(DestroyRef);
  readonly status = new FormControl<BookingStatus | ''>('', { nonNullable: true });
  readonly bookings = signal<Booking[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  // Status changes request server filtering instead of assuming the loaded list is complete.
  constructor() {
    this.destroyRef.onDestroy(() => this.request?.unsubscribe());
    this.status.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.load());
    this.load();
  }
  // Cancel the previous status request so an older response cannot replace the selected filter.
  load() {
    this.request?.unsubscribe();
    this.loading.set(true);
    this.error.set('');
    this.request = this.api
      .all(this.status.value)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (bookings) => this.bookings.set(bookings),
        error: (error) => this.error.set(apiError(error)),
      });
  }
}
