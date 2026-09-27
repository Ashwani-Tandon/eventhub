// Saves bookings and keeps a record of which purchases are being handled or have finished.
// When the user clicks Book twice, database rules let only one request start that purchase.
using Booking.Application.Ports;
using Booking.Domain;
using EventHub.BuildingBlocks.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booking.Infrastructure;

/// <summary>Reads and saves the records used to avoid buying tickets twice for the same purchase.</summary>
public sealed class BookingRepository(
    BookingDbContext db,
    ILogger<BookingRepository> logger) : IBookingRepository, IBookingRequestRepository, IUnitOfWork
{
    private static readonly Action<ILogger, Exception?> LogPersistenceFailure =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(5102, nameof(LogPersistenceFailure)),
            "Booking persistence failed before its workflow could complete");

    /// <summary>Finds a saved booking so the app can check who owns it and whether it can be cancelled.</summary>
    public Task<Domain.Booking?> FindAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return db.Bookings.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <summary>Finds a booking that may have been saved just before the app stopped unexpectedly.</summary>
    public Task<Domain.Booking?> FindByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        return db.Bookings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken);
    }

    /// <summary>Records a new purchase, or checks whether a previous request already started or finished it.</summary>
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
        // First look for this user's purchase reference. Two requests can both arrive before it exists,
        // so the database also has a rule that only one record for this user and reference can be saved.
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
            // Save "this purchase is being handled" before the app holds seats or processes payment.
            await db.SaveChangesAsync(cancellationToken);
            // Stop automatically watching this copy. Later checks read the database's latest record,
            // and we do not want an old copy accidentally saved over newer progress.
            db.Entry(request).State = EntityState.Detached;
            return new BookingRequestClaim(BookingRequestClaimOutcome.Acquired, request);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // These error numbers mean another request saved the same purchase reference first.
            // Discard our attempted record and read theirs; we must not also book the tickets.
            db.ChangeTracker.Clear();
            var winner = await FindRequestAsync(userId, idempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException("The winning booking request claim could not be loaded.");
            return await EvaluateExistingAsync(winner, eventId, quantity, simulatePaymentFailure,
                now, leaseDuration, cancellationToken);
        }
    }

    /// <summary>Marks the purchase finished and remembers which booking to show when the user repeats it.</summary>
    public async Task<Result> CompleteAsync(
        BookingRequest request,
        int bookingId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // Save "finished" and the booking number together, so a repeat knows which booking to show.
        var changed = await db.BookingRequests
            .Where(x => x.Id == request.Id && x.State == BookingRequestStates.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.BookingId, bookingId)
                .SetProperty(x => x.State, BookingRequestStates.Completed)
                .SetProperty(x => x.UpdatedAt, now)
                .SetProperty(x => x.LeaseExpiresAt, now), cancellationToken);
        // One changed record means the purchase was successfully marked finished.
        return changed == 1 ? Result.Success() : Result.Failure(BookingErrors.PersistenceUnavailable);
    }

    /// <summary>Removes the unfinished purchase record after the caller has returned any held seats.</summary>
    public async Task<Result> DeleteAsync(BookingRequest request, CancellationToken cancellationToken)
    {
        var changed = await db.BookingRequests.Where(x => x.Id == request.Id && x.State == BookingRequestStates.Processing)
            .ExecuteDeleteAsync(cancellationToken);
        return changed == 1 ? Result.Success() : Result.Failure(BookingErrors.PersistenceUnavailable);
    }

    /// <summary>Prepares a new booking; the caller still needs SaveAsync to write it to the database.</summary>
    public void Add(Domain.Booking booking)
    {
        db.Bookings.Add(booking);
    }

    /// <summary>Saves the prepared changes, or reports a database problem so the caller can handle the failure.</summary>
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

    /// <summary>Looks up the latest purchase record using the user and their purchase reference.</summary>
    private Task<BookingRequest?> FindRequestAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return db.BookingRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    /// <summary>Checks whether this is the same purchase and whether to show its booking, wait, or continue it.</summary>
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
            // The user sent the same reference with different purchase details. Refuse the change.
            return new BookingRequestClaim(BookingRequestClaimOutcome.Mismatch, request);
        }

        if (request.State == BookingRequestStates.Completed)
        {
            return new BookingRequestClaim(BookingRequestClaimOutcome.Completed, request);
        }

        if (request.LeaseExpiresAt > now)
        {
            // The first request still has time to finish. Tell this second request to wait.
            return new BookingRequestClaim(BookingRequestClaimOutcome.Processing, request);
        }

        // The first request's two minutes have passed. A repeat can try to finish the purchase.
        // Check the deadline and move it forward in one database action: only one repeat gets the turn.
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

        // Another request got the turn or finished while we were checking. Read its latest progress.
        var current = await FindRequestAsync(request.UserId, request.IdempotencyKey, cancellationToken)
            ?? throw new InvalidOperationException("The booking request claim disappeared while it was being observed.");
        return new BookingRequestClaim(
            current.State == BookingRequestStates.Completed
                ? BookingRequestClaimOutcome.Completed
                : BookingRequestClaimOutcome.Processing,
            current);
    }
}
