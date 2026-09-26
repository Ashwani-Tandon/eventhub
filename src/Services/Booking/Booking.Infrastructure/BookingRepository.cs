// Implements tracked writes for purchase creation and cancellation recovery.
// SQL faults return an availability Result so creation can compensate its Catalog reservation.
using Booking.Application.Ports;
using Booking.Domain;
using EventHub.BuildingBlocks.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booking.Infrastructure;

/// <summary>Saves each local state transition atomically through EF Core's SaveChanges transaction.</summary>
public sealed class BookingRepository(
    BookingDbContext db,
    ILogger<BookingRepository> logger) : IBookingRepository, IUnitOfWork
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
}
