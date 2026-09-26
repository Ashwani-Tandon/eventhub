// Defines the purchase information returned to booking callers.
// Copied event facts and the pending flag let the UI display bookings without querying Catalog.
namespace Booking.Application.Contracts;

/// <summary>A safe booking response, including cancellation state and purchase-time event facts.</summary>
public sealed record BookingDto(
    int Id,
    Guid UserId,
    int EventId,
    string EventTitle,
    DateTimeOffset EventStartsAt,
    Guid OrganizerId,
    int Quantity,
    decimal UnitPrice,
    decimal Total,
    string Status,
    string PaymentRef,
    Guid ReservationId,
    bool SeatReleasePending,
    DateTimeOffset CreatedAt);

/// <summary>Handwritten mapping keeps persistence entities out of API responses.</summary>
public static class BookingMappings
{
    /// <summary>Copies the saved aggregate's purchase and recovery facts into a response.</summary>
    public static BookingDto ToDto(Domain.Booking booking)
    {
        return new BookingDto(
            booking.Id, booking.UserId, booking.EventId, booking.EventTitle, booking.EventStartsAt,
            booking.OrganizerId, booking.Quantity, booking.UnitPrice, booking.Total, booking.Status,
            booking.PaymentRef, booking.ReservationId, booking.SeatReleasePending, booking.CreatedAt);
    }
}
