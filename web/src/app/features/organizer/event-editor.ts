// This typed editor submits Catalog's editable fields and the opened rowVersion.
// Client validation explains mistakes early; Catalog still owns domain and concurrency rules.
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  ValidatorFn,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { CatalogApiService } from '../../core/api/catalog-api.service';
import { CATEGORIES, EventItem } from '../../core/models/event.models';
import { apiError } from '../../shared/api-error';
@Component({
  selector: 'app-event-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule],
  templateUrl: './event-editor.html',
})
export class EventEditor {
  private readonly api = inject(CatalogApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackbar = inject(MatSnackBar);
  readonly id = Number(this.route.snapshot.paramMap.get('id')) || null;
  readonly categories = CATEGORIES;
  private original: EventItem | null = null;
  private originalLocal = '';
  readonly loading = signal(false);
  readonly pending = signal(false);
  readonly error = signal('');
  readonly conflict = signal(false);
  // Historical dates may remain unchanged on edit; newly selected dates must be future.
  private readonly futureDate: ValidatorFn = (control) => {
    const value = String(control.value ?? '');
    if (this.id && value === this.originalLocal) return null;
    const date = new Date(value).getTime();
    return Number.isFinite(date) && date > Date.now() ? null : { future: true };
  };
  readonly form = new FormGroup({
    title: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/\S/), Validators.maxLength(120)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(2000)],
    }),
    category: new FormControl('Tech', { nonNullable: true, validators: [Validators.required] }),
    venue: new FormControl('', { nonNullable: true }),
    city: new FormControl('', { nonNullable: true }),
    startsAt: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, this.futureDate],
    }),
    price: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)],
    }),
    capacity: new FormControl(100, {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.min(1),
        Validators.max(10000),
        Validators.pattern(/^\d+$/),
      ],
    }),
  });
  // Create begins blank; editing first loads the exact server version and booked capacity.
  constructor() {
    if (this.id) this.load();
  }
  // Reload deliberately discards unsaved changes after an edit conflict.
  load() {
    if (!this.id) return;
    this.loading.set(true);
    this.error.set('');
    this.conflict.set(false);
    this.api
      .get(this.id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (event) => {
          this.original = event;
          this.originalLocal = this.localDate(event.startsAt);
          this.form.setValue({
            title: event.title,
            description: event.description,
            category: event.category,
            venue: event.venue,
            city: event.city,
            startsAt: this.originalLocal,
            price: event.price,
            capacity: event.capacity,
          });
          this.form.controls.capacity.setValidators([
            Validators.required,
            Validators.min(Math.max(1, event.seatsBooked)),
            Validators.max(10000),
            Validators.pattern(/^\d+$/),
          ]);
          this.form.controls.capacity.updateValueAndValidity();
          this.form.controls.startsAt.updateValueAndValidity();
        },
        error: (error) => this.error.set(apiError(error)),
      });
  }
  // Keep the original timestamp when unchanged, avoiding accidental seconds/timezone edits.
  save() {
    if (this.pending() || this.loading() || (this.id && !this.original)) return;
    this.form.controls.startsAt.updateValueAndValidity();
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const body = {
      ...v,
      startsAt:
        this.original && v.startsAt === this.originalLocal
          ? this.original.startsAt
          : new Date(v.startsAt).toISOString(),
    };
    const request = this.id
      ? this.api.update(this.id, body, this.original!.rowVersion)
      : this.api.create(body);
    this.pending.set(true);
    this.error.set('');
    this.conflict.set(false);
    request.pipe(finalize(() => this.pending.set(false))).subscribe({
      next: () => {
        this.snackbar.open(this.id ? 'Event updated.' : 'Event created.', 'Close', {
          duration: 5000,
        });
        void this.router.navigate(['/my-events']);
      },
      error: (error) => {
        this.error.set(apiError(error));
        this.conflict.set(error.status === 409);
      },
    });
  }
  // Format an instant for datetime-local without shifting the visible local wall time.
  private localDate(value: string) {
    const date = new Date(value);
    return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  }
  // Touched state keeps warnings from appearing before the user starts editing.
  invalid(field: keyof typeof this.form.controls) {
    const control = this.form.controls[field];
    return control.touched && control.invalid;
  }
  // Capacity cannot remove seats already sold, even when the lower bound exceeds one.
  minimumCapacity() {
    return Math.max(1, this.original?.seatsBooked ?? 1);
  }
}
