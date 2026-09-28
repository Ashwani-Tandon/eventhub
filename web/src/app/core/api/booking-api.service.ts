// This adapter is the single browser boundary for Booking requests and statistics.
// It sends no price or user identity on purchase; the server supplies trusted values.
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Booking, BookingStatus, SalesStats } from '../models/booking.models';
@Injectable({ providedIn: 'root' })
export class BookingApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/booking/bookings';
  // Submit one purchase; automatic write retries are deliberately absent.
  book(eventId: number, quantity: number, simulatePaymentFailure: boolean, idempotencyKey: string) {
    return this.http.post<Booking>(
      this.base,
      { eventId, quantity, simulatePaymentFailure },
      {
        headers: { 'Idempotency-Key': idempotencyKey },
      },
    );
  }
  // History is read from Booking without needing Catalog's availability.
  mine() {
    return this.http.get<Booking[]>(`${this.base}/mine`);
  }
  // Repeat a pending cancellation to finish returning its seats safely.
  cancel(id: number) {
    return this.http.post<Booking>(`${this.base}/${id}/cancel`, {});
  }
  // Only Admin may list all users' purchases.
  all(status: BookingStatus | '') {
    return this.http.get<Booking[]>(this.base, { params: status ? { status } : {} });
  }
  // The API scopes Organizer totals and returns combined totals for Admin.
  stats() {
    return this.http.get<SalesStats>(`${this.base}/stats`);
  }
}
