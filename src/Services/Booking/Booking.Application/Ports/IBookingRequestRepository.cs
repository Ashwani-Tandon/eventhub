// Defines the atomic claim and recovery operations required by idempotent booking creation.
// Infrastructure implements their concurrency details while the handler reasons in explicit outcomes.
using Booking.Domain;
using EventHub.BuildingBlocks.Results;

namespace Booking.Application.Ports;

/// <summary>Describes whether this caller owns work, must wait, or can return a completed booking.</summary>
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
    Task<BookingRequestClaim> ClaimAsync(
        Guid userId,
        string idempotencyKey,
        int eventId,
        int quantity,
        bool simulatePaymentFailure,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task<Result> CompleteAsync(
        BookingRequest request,
        int bookingId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<Result> DeleteAsync(BookingRequest request, CancellationToken cancellationToken);
}
