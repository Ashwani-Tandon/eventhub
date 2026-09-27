// Names expected Catalog failures without depending on HTTP.
// Handlers return these errors and ServiceDefaults translates their types to status codes.
using EventHub.BuildingBlocks.Results;

namespace Catalog.Domain;

/// <summary>
/// Names expected Catalog failures without depending on HTTP. Handlers return these errors and ServiceDefaults translates their types to status codes.
/// </summary>
public static class EventErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Event.NotFound",
        "Event not found.");

    public static readonly Error Unauthenticated = Error.Unauthorized(
        "Event.Unauthenticated",
        "Authentication is required.");

    public static readonly Error Forbidden = Error.Forbidden(
        "Event.Forbidden",
        "You do not own this event.");

    public static readonly Error HasBookings = Error.Conflict(
        "Event.HasBookings",
        "An event with booked seats cannot be deleted.");

    public static readonly Error ConcurrencyConflict = Error.Conflict(
        "Event.ConcurrencyConflict",
        "This event was changed by someone else — reload and try again.");

    public static readonly Error NotEnoughSeats = Error.Conflict(
        "Reservation.NotEnoughSeats",
        "Not enough seats are available.");

    public static readonly Error ReplayMismatch = Error.Conflict(
        "Reservation.ReplayMismatch",
        "This reservation id was already used with different details.");

    public static readonly Error ReservationForbidden = Error.Forbidden(
        "Reservation.Forbidden",
        "This reservation belongs to another user.");

    /// <summary>
    /// Creates a validation error with a field message for the HTTP response.
    /// </summary>
    public static Error Invalid(
        string field,
        string message)
    {
        return Error.Validation("Event.Invalid", "The event is invalid.", new Dictionary<string, string[]> { [field] = [message] });
    }
}
