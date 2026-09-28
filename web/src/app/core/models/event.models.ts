// These contracts mirror Catalog's public event responses and editable inputs.
// Keeping seat totals and rowVersion explicit prevents the UI from inventing server facts.
export const CATEGORIES = ['Music', 'Tech', 'Sports', 'Comedy', 'Workshop'] as const;
export interface EventInput {
  title: string;
  description: string;
  category: string;
  venue: string;
  city: string;
  startsAt: string;
  price: number;
  capacity: number;
}
export interface EventItem extends EventInput {
  id: number;
  organizerId: string;
  seatsBooked: number;
  seatsLeft: number;
  rowVersion: string;
}
export interface EventPage {
  items: EventItem[];
  total: number;
}
export interface EventFilters {
  search?: string;
  category?: string;
  city?: string;
  from?: string;
  to?: string;
  maxPrice?: number;
  page: number;
  pageSize: number;
}
