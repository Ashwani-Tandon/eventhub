// Records the durable identity and progress of one idempotent booking request.
// It is claimed before external side effects so retries reuse the same reservation and payment identities.
using EventHub.BuildingBlocks.Domain;

namespace Booking.Domain;

/// <summary>Owns the immutable request fingerprint and the recoverable processing state.</summary>
public sealed class BookingRequest : Entity<long>
{
    private BookingRequest() : base(0)
    {
    }

    public Guid UserId { get; private set; }
    public string IdempotencyKey { get; private set; } = "";
    public int EventId { get; private set; }
    public int Quantity { get; private set; }
    public bool SimulatePaymentFailure { get; private set; }
    public Guid ReservationId { get; private set; }
    public string State { get; private set; } = BookingRequestStates.Processing;
    public int? BookingId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset LeaseExpiresAt { get; private set; }

    /// <summary>Creates the winner's pre-side-effect claim with one stable reservation identity.</summary>
    public static BookingRequest Create(
        Guid userId,
        string idempotencyKey,
        int eventId,
        int quantity,
        bool simulatePaymentFailure,
        DateTimeOffset now,
        TimeSpan leaseDuration)
    {
        return new BookingRequest
        {
            UserId = userId,
            IdempotencyKey = idempotencyKey,
            EventId = eventId,
            Quantity = quantity,
            SimulatePaymentFailure = simulatePaymentFailure,
            ReservationId = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now,
            LeaseExpiresAt = now.Add(leaseDuration)
        };
    }

    /// <summary>Prevents one key from being replayed with different purchase inputs.</summary>
    public bool Matches(int eventId, int quantity, bool simulatePaymentFailure)
    {
        return EventId == eventId && Quantity == quantity && SimulatePaymentFailure == simulatePaymentFailure;
    }

}

/// <summary>Stable persisted values used to distinguish active and completed claims.</summary>
public static class BookingRequestStates
{
    public const string Processing = "Processing";
    public const string Completed = "Completed";
}
