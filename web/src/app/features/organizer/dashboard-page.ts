// The dashboard combines Booking's scoped sales with Catalog's managed-event capacity.
// Independent requests let sales remain readable when only event capacity fails to load.
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DecimalPipe, PercentPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { NgxEchartsDirective, provideEchartsCore } from 'ngx-echarts';
import * as echarts from 'echarts/core';
import { BarChart, PieChart } from 'echarts/charts';
import {
  GridComponent,
  TooltipComponent,
  LegendComponent,
  AriaComponent,
} from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { EChartsCoreOption } from 'echarts/core';
import { finalize } from 'rxjs';
import { BookingApiService } from '../../core/api/booking-api.service';
import { CatalogApiService } from '../../core/api/catalog-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { SalesStats } from '../../core/models/booking.models';
import { EventItem } from '../../core/models/event.models';
import { apiError } from '../../shared/api-error';
// Register only the chart types/rendering features used here; lazy routing keeps them out of login.
echarts.use([
  BarChart,
  PieChart,
  GridComponent,
  TooltipComponent,
  LegendComponent,
  AriaComponent,
  CanvasRenderer,
]);
@Component({
  selector: 'app-dashboard-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DecimalPipe, PercentPipe, MatButtonModule, NgxEchartsDirective],
  providers: [provideEchartsCore({ echarts })],
  templateUrl: './dashboard-page.html',
})
export class DashboardPage {
  private readonly bookingApi = inject(BookingApiService);
  private readonly catalogApi = inject(CatalogApiService);
  readonly auth = inject(AuthService);
  readonly stats = signal<SalesStats | null>(null);
  readonly events = signal<EventItem[]>([]);
  readonly statsLoading = signal(false);
  readonly eventsLoading = signal(false);
  readonly statsError = signal('');
  readonly eventsError = signal('');
  readonly averageFill = computed(() => {
    const events = this.events();
    return events.length
      ? events.reduce((total, event) => total + event.seatsBooked / event.capacity, 0) /
          events.length
      : 0;
  });
  readonly monthly = computed<EChartsCoreOption>(() => ({
    color: ['#257450'],
    aria: { enabled: true },
    tooltip: {
      trigger: 'axis',
      valueFormatter: (value: unknown) => `₹${Number(value).toLocaleString('en-IN')}`,
    },
    grid: { left: 70, right: 20, bottom: 40, top: 20 },
    xAxis: { type: 'category', data: this.stats()?.revenueByMonth.map((item) => item.month) ?? [] },
    yAxis: { type: 'value' },
    series: [
      {
        type: 'bar',
        barMaxWidth: 42,
        data: this.stats()?.revenueByMonth.map((item) => item.revenue) ?? [],
      },
    ],
  }));
  readonly top = computed<EChartsCoreOption>(() => ({
    color: ['#86a96b'],
    aria: { enabled: true },
    tooltip: { trigger: 'axis' },
    grid: { left: 170, right: 25, bottom: 35, top: 10 },
    yAxis: {
      type: 'category',
      inverse: true,
      data: this.stats()?.topEvents.map((item) => item.title) ?? [],
      axisLabel: { width: 145, overflow: 'truncate' },
    },
    xAxis: { type: 'value', minInterval: 1 },
    series: [{ type: 'bar', data: this.stats()?.topEvents.map((item) => item.tickets) ?? [] }],
  }));
  readonly status = computed<EChartsCoreOption>(() => ({
    color: ['#257450', '#e4a190'],
    aria: { enabled: true },
    tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
    legend: { bottom: 0 },
    series: [
      {
        type: 'pie',
        radius: ['45%', '70%'],
        center: ['50%', '45%'],
        label: { show: false },
        data:
          this.stats()?.statusCounts.map((item) => ({ name: item.status, value: item.count })) ??
          [],
      },
    ],
  }));
  // Load both scopes when entering the dashboard; the APIs enforce ownership.
  constructor() {
    this.refresh();
  }
  // A refresh reflects purchases since the last visit without reusing stale chart values.
  refresh() {
    if (this.statsLoading() || this.eventsLoading()) return;
    this.loadStats();
    this.loadEvents();
  }
  // Only Booking data powers revenue, tickets, booking counts, and all three charts.
  private loadStats() {
    this.statsLoading.set(true);
    this.statsError.set('');
    this.bookingApi
      .stats()
      .pipe(finalize(() => this.statsLoading.set(false)))
      .subscribe({
        next: (stats) => this.stats.set(stats),
        error: (error) => this.statsError.set(apiError(error)),
      });
  }
  // Average fill is the arithmetic mean of each managed event's booked/capacity ratio.
  private loadEvents() {
    this.eventsLoading.set(true);
    this.eventsError.set('');
    this.catalogApi
      .mine()
      .pipe(finalize(() => this.eventsLoading.set(false)))
      .subscribe({
        next: (events) => this.events.set(events),
        error: (error) => this.eventsError.set(apiError(error)),
      });
  }
}
