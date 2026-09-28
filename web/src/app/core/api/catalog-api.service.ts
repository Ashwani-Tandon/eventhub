// This adapter sends all browser requests to Catalog through the gateway proxy.
// Components work with typed DTOs, while Catalog enforces ownership and seat rules.
import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { EventFilters, EventInput, EventItem, EventPage } from '../models/event.models';
@Injectable({ providedIn: 'root' })
export class CatalogApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/catalog/events';
  // Omit empty filters so Catalog keeps its normal upcoming-events default.
  search(filters: EventFilters) {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== '') params = params.set(key, String(value));
    }
    return this.http.get<EventPage>(this.base, { params });
  }
  // Read the current seat count and edit version for one public event.
  get(id: number) {
    return this.http.get<EventItem>(`${this.base}/${id}`);
  }
  // Catalog scopes this list to the organizer, or returns all events for Admin.
  mine() {
    return this.http.get<EventItem[]>(`${this.base}/mine`);
  }
  // The server assigns ownership to the signed-in creator.
  create(body: EventInput) {
    return this.http.post<EventItem>(this.base, body);
  }
  // Send the version opened by the editor so a newer save is not overwritten.
  update(id: number, body: EventInput, rowVersion: string) {
    return this.http.put<EventItem>(`${this.base}/${id}`, { ...body, rowVersion });
  }
  // Catalog rejects deletion when seats are still booked.
  delete(id: number) {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
