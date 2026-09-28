// This dialog collects ticket quantity and shows the server-priced total.
// Purchase feedback keeps failed requests visible so the user can make an informed choice.
import { ChangeDetectionStrategy, Component, inject, isDevMode, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { CurrencyPipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { BookingApiService } from '../../core/api/booking-api.service';
import { EventItem } from '../../core/models/event.models';
import { apiError } from '../../shared/api-error';
@Component({
  selector: 'app-booking-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, ReactiveFormsModule, MatDialogModule, MatButtonModule],
  templateUrl: './booking-dialog.html',
})
export class BookingDialog {
  readonly event = inject<EventItem>(MAT_DIALOG_DATA);
  private readonly api = inject(BookingApiService);
  private readonly ref = inject(MatDialogRef<BookingDialog>);
  private readonly snackbar = inject(MatSnackBar);
  readonly development = isDevMode();
  readonly pending = signal(false);
  readonly error = signal('');
  // Keep the identity of the submitted payload so Confirm again can recover a lost response.
  private attempt: { key: string; quantity: number; simulatePaymentFailure: boolean } | null = null;
  readonly max = Math.min(10, this.event.seatsLeft);
  readonly form = new FormGroup({
    quantity: new FormControl(1, {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.min(1),
        Validators.max(this.max),
        Validators.pattern(/^\d+$/),
      ],
    }),
    simulatePaymentFailure: new FormControl(false, { nonNullable: true }),
  });
  // Use the captured Catalog price for the preview; Booking recalculates trusted price.
  total() {
    return this.form.controls.quantity.value * this.event.price;
  }
  // Disable closing during purchase so the user sees its result before leaving.
  confirm() {
    if (this.pending()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const simulatePaymentFailure = this.development && v.simulatePaymentFailure;
    // Changed ticket details mean a different purchase; unchanged details reuse the same key.
    if (
      !this.attempt ||
      this.attempt.quantity !== v.quantity ||
      this.attempt.simulatePaymentFailure !== simulatePaymentFailure
    ) {
      this.attempt = { key: crypto.randomUUID(), quantity: v.quantity, simulatePaymentFailure };
    }
    this.pending.set(true);
    this.error.set('');
    this.ref.disableClose = true;
    this.form.disable();
    this.api
      .book(this.event.id, v.quantity, simulatePaymentFailure, this.attempt.key)
      .pipe(
        finalize(() => {
          this.pending.set(false);
          this.ref.disableClose = false;
          this.form.enable();
        }),
      )
      .subscribe({
        next: () => {
          this.snackbar.open('Booking confirmed. Your tickets are in My Bookings.', 'Close', {
            duration: 6000,
          });
          this.ref.close(true);
        },
        error: (error: unknown) => {
          // A clear validation/missing-event/payment rejection ends this attempt without a booking.
          // The next Confirm starts a new purchase; ambiguous failures keep the key for recovery.
          if (error instanceof HttpErrorResponse && [400, 404, 422].includes(error.status)) {
            this.attempt = null;
          }
          const message = apiError(error);
          this.error.set(message);
          this.snackbar.open(message, 'Close', { duration: 7000 });
        },
      });
  }
}
