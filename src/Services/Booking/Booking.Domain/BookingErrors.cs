// Names expected failures during booking creation and cancellation.
// Handlers return these errors; the API maps them to HTTP without placing HTTP dependencies in Domain.
using EventHub.BuildingBlocks.Results;

namespace Booking.Domain;

/// <summary>Stable error codes and messages for the booking lifecycle and its dependencies.</summary>
public static class BookingErrors
{
    public static readonly Error Unauthenticated = Error.Unauthorized("Booking.Unauthenticated", "Authentication is required.");
    public static readonly Error NotFound = Error.NotFound("Booking.NotFound", "Booking not found.");
    public static readonly Error Forbidden = Error.Forbidden("Booking.Forbidden", "This booking belongs to another user.");
    public static readonly Error EventNotFound = Error.NotFound("Booking.EventNotFound", "Event not found.");
    public static readonly Error CatalogUnavailable = Error.Unavailable("Booking.CatalogUnavailable", "Catalog service unavailable");
    public static readonly Error PersistenceUnavailable = Error.Unavailable("Booking.PersistenceUnavailable", "Booking database unavailable");
    public static readonly Error RequestInProgress = Error.Unavailable("Booking.RequestInProgress", "This booking request is still processing. Try again shortly.");
    public static readonly Error IdempotencyMismatch = Error.Conflict("Booking.IdempotencyMismatch", "This idempotency key was already used with different booking details.");
    public static readonly Error PaymentFailed = Error.Unprocessable("Booking.PaymentFailed", "Payment failed. Reserved seats have been released.");
    public static readonly Error NotConfirmed = Error.Validation("Booking.NotConfirmed", "Only confirmed bookings can begin cancellation.");
    public static readonly Error EventStarted = Error.Validation("Booking.EventStarted", "The event has already started.",
        new Dictionary<string, string[]> { ["EventId"] = ["The event must not have started."] });
    public static readonly Error InvalidQuantity = Error.Validation("Booking.InvalidQuantity", "Quantity must be between 1 and 10.",
        new Dictionary<string, string[]> { ["Quantity"] = ["Quantity must be between 1 and 10."] });
}
