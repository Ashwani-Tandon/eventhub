// Lists the database actions Booking needs to recognize a user's repeated purchase.
// The handler asks these questions; the repository handles saving and looking up the answers.
using Booking.Domain;
using EventHub.BuildingBlocks.Results;

namespace Booking.Application.Ports;

/// <summary>Answers: start the purchase, wait for it, show the finished booking, or reject changed details.</summary>
public enum BookingRequestClaimOutcome
{
    Acquired,
    Processing,
    Completed,
    Mismatch
}

/// <summary>Carries the durable request identity and the result of trying to claim it.</summary>
public sealed record BookingRequestClaim(
    BookingRequestClaimOutcome Outcome,
    BookingRequest Request);

/// <summary>Persistence boundary for the pre-side-effect request claim.</summary>
public interface IBookingRequestRepository
{
    /// <summary>Checks this purchase reference; Acquired means this request may start or finish the purchase.</summary>
    Task<BookingRequestClaim> ClaimAsync(
        Guid userId,
        string idempotencyKey,
        int eventId,
        int quantity,
        bool simulatePaymentFailure,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>Remembers the booking number and marks the purchase finished, so another click shows the same booking.</summary>
    Task<Result> CompleteAsync(
        BookingRequest request,
        int bookingId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>Removes an unfinished purchase after its seats are returned, allowing the user to try again.</summary>
    Task<Result> DeleteAsync(BookingRequest request, CancellationToken cancellationToken);
}
