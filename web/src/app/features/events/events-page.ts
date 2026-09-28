// Public browsing uses Catalog's filters, total count, and one-based pages.
// Switching requests cancels old searches so slow results cannot replace newer filters.
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, finalize, Subscription } from 'rxjs';
import { CatalogApiService } from '../../core/api/catalog-api.service';
import { CATEGORIES, EventItem } from '../../core/models/event.models';
import { apiError } from '../../shared/api-error';
@Component({
  selector: 'app-events-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule, RouterLink, MatButtonModule],
  templateUrl: './events-page.html',
})
export class EventsPage {
  private readonly api = inject(CatalogApiService);
  private readonly destroyRef = inject(DestroyRef);
  private request?: Subscription;
  readonly categories = CATEGORIES;
  readonly events = signal<EventItem[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = 12;
  readonly loading = signal(false);
  readonly error = signal('');
  readonly form = new FormGroup({
    search: new FormControl('', { nonNullable: true }),
    category: new FormControl('', { nonNullable: true }),
    city: new FormControl('', { nonNullable: true }),
    from: new FormControl('', { nonNullable: true }),
    to: new FormControl('', { nonNullable: true }),
    maxPrice: new FormControl<number | null>(null, Validators.min(0)),
  });
  // Search waits 300 ms while typing; navigation cleanup also cancels outstanding HTTP.
  constructor() {
    this.form.valueChanges.pipe(debounceTime(300), takeUntilDestroyed()).subscribe(() => {
      this.page.set(1);
      this.load();
    });
    this.destroyRef.onDestroy(() => this.request?.unsubscribe());
    this.load();
  }
  // Map local calendar dates to inclusive UTC boundaries before calling Catalog.
  load() {
    this.request?.unsubscribe();
    const v = this.form.getRawValue();
    if (this.form.invalid || (v.from && v.to && v.to < v.from)) {
      this.error.set('Choose a valid price and a date range with the end after the start.');
      this.events.set([]);
      this.total.set(0);
      return;
    }
    this.error.set('');
    this.loading.set(true);
    this.request = this.api
      .search({
        search: v.search.trim(),
        category: v.category,
        city: v.city.trim(),
        from: v.from ? new Date(`${v.from}T00:00:00`).toISOString() : undefined,
        to: v.to ? new Date(`${v.to}T23:59:59.999`).toISOString() : undefined,
        maxPrice: v.maxPrice ?? undefined,
        page: this.page(),
        pageSize: this.pageSize,
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (result) => {
          this.events.set(result.items);
          this.total.set(result.total);
        },
        error: (error) => {
          this.events.set([]);
          this.total.set(0);
          this.error.set(apiError(error));
        },
      });
  }
  // Clearing all fields restores Catalog's upcoming-events view and first page.
  clear() {
    this.form.reset();
  }
  // Clamp page changes to the current total; filters always return to page one.
  changePage(delta: number) {
    const page = this.page() + delta;
    if (page < 1 || page > this.pages()) return;
    this.page.set(page);
    this.load();
  }
  // Catalog's total covers every matching page rather than only visible cards.
  pages() {
    return Math.max(1, Math.ceil(this.total() / this.pageSize));
  }
}
