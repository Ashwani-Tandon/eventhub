// These contracts describe chat text and service-derived action proposals.
// A proposal is a preview, never proof of a purchase; the Yes button submits through Booking.
export interface ChatTurn {
  role: 'user' | 'assistant';
  content: string;
}
export interface ActionProposal {
  kind: 'book' | 'cancel';
  eventId: number;
  bookingId: number | null;
  eventTitle: string;
  eventStartsAt: string;
  quantity: number;
  unitPrice: number;
  total: number;
}
export interface ChatReply {
  reply: string;
  action: ActionProposal | null;
}
