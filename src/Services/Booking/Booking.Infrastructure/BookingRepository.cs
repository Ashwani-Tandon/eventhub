// Implements booking writes plus atomic idempotency claim, observation, and lease recovery operations.
// SQL faults return expected results so the creation workflow can compensate or safely resume.
using Booking.Application.Ports;
using Booking.Domain;
using EventHub.BuildingBlocks.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booking.Infrastructure;

/// <summary>Persists bookings and coordinates database-enforced ownership of idempotent request work.</summary>
public sealed class BookingRepository(
    BookingDbContext db,
    ILogger<BookingRepository> logger) : IBookingRepository, IBookingRequestRepository, IUnitOfWork
{
    private static readonly Action<ILogger, Exception?> LogPersistenceFailure =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(5102, nameof(LogPersistenceFailure)),
            "Booking persistence failed before its workflow could complete");

    /// <summary>Loads the owned aggregate whose cancellation rules must run.</summary>
    public Task<Domain.Booking?> FindAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return db.Bookings.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <summary>Loads the confirmed booking written during the claim-completion crash window.</summary>
    public Task<Domain.Booking?> FindByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        return db.Bookings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken);
    }

    /// <summary>Atomically inserts the first claim or takes over an expired processing lease.</summary>
    public async Task<BookingRequestClaim> ClaimAsync(
        Guid userId,
        string idempotencyKey,
        int eventId,
        int quantity,
        bool simulatePaymentFailure,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var existing = await FindRequestAsync(userId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return await EvaluateExistingAsync(existing, eventId, quantity, simulatePaymentFailure,
                now, leaseDuration, cancellationToken);
        }

        var request = BookingRequest.Create(userId, idempotencyKey, eventId, quantity,
            simulatePaymentFailure, now, leaseDuration);
        db.BookingRequests.Add(request);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            db.Entry(request).State = EntityState.Detached;
            return new BookingRequestClaim(BookingRequestClaimOutcome.Acquired, request);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.ChangeTracker.Clear();
            var winner = await FindRequestAsync(userId, idempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException("The winning booking request claim could not be loaded.");
            return await EvaluateExistingAsync(winner, eventId, quantity, simulatePaymentFailure,
                now, leaseDuration, cancellationToken);
        }
    }

    /// <summary>Persists the link to the booking so every later replay returns the original response.</summary>
    public async Task<Result> CompleteAsync(
        BookingRequest request,
        int bookingId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var changed = await db.BookingRequests
            .Where(x => x.Id == request.Id && x.State == BookingRequestStates.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.BookingId, bookingId)
                .SetProperty(x => x.State, BookingRequestStates.Completed)
                .SetProperty(x => x.UpdatedAt, now)
                .SetProperty(x => x.LeaseExpiresAt, now), cancellationToken);
        return changed == 1 ? Result.Success() : Result.Failure(BookingErrors.PersistenceUnavailable);
    }

    /// <summary>Removes a known-failed claim only after any required compensation has succeeded.</summary>
    public async Task<Result> DeleteAsync(BookingRequest request, CancellationToken cancellationToken)
    {
        var changed = await db.BookingRequests.Where(x => x.Id == request.Id && x.State == BookingRequestStates.Processing)
            .ExecuteDeleteAsync(cancellationToken);
        return changed == 1 ? Result.Success() : Result.Failure(BookingErrors.PersistenceUnavailable);
    }

    /// <summary>Tracks the confirmed purchase for insertion at the handler's save boundary.</summary>
    public void Add(Domain.Booking booking)
    {
        db.Bookings.Add(booking);
    }

    /// <summary>Commits tracked changes together; expected SQL faults let callers handle cleanup explicitly.</summary>
    public async Task<Result> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception exception) when (exception is DbUpdateException or SqlException)
        {
            LogPersistenceFailure(logger, exception);
            return Result.Failure(BookingErrors.PersistenceUnavailable);
        }
    }

    private Task<BookingRequest?> FindRequestAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return db.BookingRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    private async Task<BookingRequestClaim> EvaluateExistingAsync(
        BookingRequest request,
        int eventId,
        int quantity,
        bool simulatePaymentFailure,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        if (!request.Matches(eventId, quantity, simulatePaymentFailure))
        {
            return new BookingRequestClaim(BookingRequestClaimOutcome.Mismatch, request);
        }

        if (request.State == BookingRequestStates.Completed)
        {
            return new BookingRequestClaim(BookingRequestClaimOutcome.Completed, request);
        }

        if (request.LeaseExpiresAt > now)
        {
            return new BookingRequestClaim(BookingRequestClaimOutcome.Processing, request);
        }

        var renewedUntil = now.Add(leaseDuration);
        var changed = await db.BookingRequests
            .Where(x => x.Id == request.Id && x.State == BookingRequestStates.Processing && x.LeaseExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.UpdatedAt, now)
                .SetProperty(x => x.LeaseExpiresAt, renewedUntil), cancellationToken);
        if (changed == 1)
        {
            var renewed = await FindRequestAsync(request.UserId, request.IdempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException("The renewed booking request claim could not be loaded.");
            return new BookingRequestClaim(BookingRequestClaimOutcome.Acquired, renewed);
        }

        var current = await FindRequestAsync(request.UserId, request.IdempotencyKey, cancellationToken)
            ?? throw new InvalidOperationException("The booking request claim disappeared while it was being observed.");
        return new BookingRequestClaim(
            current.State == BookingRequestStates.Completed
                ? BookingRequestClaimOutcome.Completed
                : BookingRequestClaimOutcome.Processing,
            current);
    }
}
