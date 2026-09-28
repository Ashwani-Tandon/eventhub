// This dialog collects ticket quantity and shows the server-priced total.
// Purchase feedback keeps failed requests visible so the user can make an informed choice.
import { ChangeDetectionStrategy, Component, inject, isDevMode, signal } from '@angular/core';
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
    this.pending.set(true);
    this.error.set('');
    this.ref.disableClose = true;
    this.api
      .book(this.event.id, v.quantity, this.development && v.simulatePaymentFailure)
      .pipe(
        finalize(() => {
          this.pending.set(false);
          this.ref.disableClose = false;
        }),
      )
      .subscribe({
        next: () => {
          this.snackbar.open('Booking confirmed. Your tickets are in My Bookings.', 'Close', {
            duration: 6000,
          });
          this.ref.close(true);
        },
        error: (error) => {
          const message = apiError(error);
          this.error.set(message);
          this.snackbar.open(message, 'Close', { duration: 7000 });
        },
      });
  }
}
