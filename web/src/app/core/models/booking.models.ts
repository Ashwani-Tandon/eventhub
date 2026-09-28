// These response types describe Booking's purchase snapshots and scoped sales.
// History can display these copied event facts even when Catalog is unavailable.
export type BookingStatus = 'Confirmed' | 'Cancelled';
export interface Booking {
  id: number;
  userId: string;
  eventId: number;
  eventTitle: string;
  eventStartsAt: string;
  organizerId: string;
  quantity: number;
  unitPrice: number;
  total: number;
  status: BookingStatus;
  paymentRef: string;
  reservationId: string;
  seatReleasePending: boolean;
  createdAt: string;
}
export interface SalesStats {
  totals: { revenue: number; ticketsSold: number; bookings: number; cancelled: number };
  revenueByMonth: { month: string; revenue: number }[];
  topEvents: { eventId: number; title: string; tickets: number; revenue: number }[];
  statusCounts: { status: BookingStatus; count: number }[];
}
