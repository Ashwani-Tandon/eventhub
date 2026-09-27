// Remembers a user's purchase reference, ticket choices, and whether the purchase is finished.
// Save this before holding seats or paying, so a second click can recognize the same purchase.
using EventHub.BuildingBlocks.Domain;

namespace Booking.Domain;

/// <summary>Remembers what the user wanted to buy and how far that purchase has progressed.</summary>
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

    /// <summary>Starts the purchase record and gives its seat hold a reference we can reuse after a restart.</summary>
    public static BookingRequest Create(
        Guid userId,
        string idempotencyKey,
        int eventId,
        int quantity,
        bool simulatePaymentFailure,
        DateTimeOffset now,
        TimeSpan leaseDuration)
    {
        // Give this purchase one seat-hold reference and remember it.
        // If the app stops midway, use that same reference instead of holding seats twice.
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

    /// <summary>Checks that a repeated purchase reference still asks for the same event, tickets, and payment-demo choice.</summary>
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
