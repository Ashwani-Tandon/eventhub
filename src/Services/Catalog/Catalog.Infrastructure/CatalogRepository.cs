// Implements tracked event writes plus atomic reservation and release operations.
// Database-side conditional updates preserve capacity and idempotency under concurrent requests.
using Catalog.Application;
using Catalog.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public sealed class CatalogRepository(CatalogDbContext db) : IEventRepository, IReservationRepository, IUnitOfWork
{
    public Task<Event?> FindAsync(int id, CancellationToken cancellationToken) =>
        db.Events.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public void Add(Event eventItem) => db.Events.Add(eventItem);
    public void Remove(Event eventItem) => db.Events.Remove(eventItem);
    public async Task SaveAsync(CancellationToken cancellationToken) => await db.SaveChangesAsync(cancellationToken);

    public async Task<ReserveResult> TryReserveAsync(Guid reservationId, int eventId, Guid userId,
        int quantity, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                var existing = await db.SeatReservations.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == reservationId, cancellationToken);
                if (existing is not null) return Match(existing, eventId, userId, quantity);

                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                var changed = await db.Events.Where(x => x.Id == eventId && x.Capacity - x.SeatsBooked >= quantity)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.SeatsBooked, x => x.SeatsBooked + quantity),
                        cancellationToken);
                if (changed == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    var exists = await db.Events.AsNoTracking().AnyAsync(x => x.Id == eventId, cancellationToken);
                    return new(exists ? ReserveOutcome.NotEnoughSeats : ReserveOutcome.EventNotFound, null);
                }

                var reservation = SeatReservation.Hold(reservationId, eventId, userId, quantity, now);
                db.SeatReservations.Add(reservation);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new ReserveResult(ReserveOutcome.Success, reservation);
            });
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // The losing transaction was rolled back, including its seat increment; return the winning replay.
            db.ChangeTracker.Clear();
            var winner = await db.SeatReservations.AsNoTracking()
                .SingleAsync(x => x.Id == reservationId, cancellationToken);
            return Match(winner, eventId, userId, quantity);
        }
    }

    public async Task<ReleaseResult> ReleaseAsync(Guid reservationId, Guid userId, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var reservation = await db.SeatReservations.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == reservationId, cancellationToken);
            if (reservation is null) return new ReleaseResult(ReleaseOutcome.NoChange, null);
            if (reservation.UserId != userId) return new ReleaseResult(ReleaseOutcome.Forbidden, reservation);
            if (reservation.Status == ReservationStatuses.Released)
                return new ReleaseResult(ReleaseOutcome.NoChange, reservation);

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var changed = await db.SeatReservations
                .Where(x => x.Id == reservationId && x.UserId == userId && x.Status == ReservationStatuses.Held)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, ReservationStatuses.Released)
                    .SetProperty(x => x.ReleasedAt, now), cancellationToken);
            if (changed == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ReleaseResult(ReleaseOutcome.NoChange, reservation);
            }
            await db.Events.Where(x => x.Id == reservation.EventId && x.SeatsBooked >= reservation.Quantity)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.SeatsBooked,
                    x => x.SeatsBooked - reservation.Quantity), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ReleaseResult(ReleaseOutcome.Released, reservation);
        });
    }

    private static ReserveResult Match(SeatReservation existing, int eventId, Guid userId, int quantity) =>
        existing.EventId == eventId && existing.UserId == userId && existing.Quantity == quantity
            ? new(ReserveOutcome.Success, existing)
            : new(ReserveOutcome.ReplayMismatch, existing);
}
