// Projects booking screens and sales aggregates from bookingdb without contacting Catalog.
// Organizer ownership and event labels come from snapshots recorded when the purchase was made.
using System.Globalization;
using Booking.Application.Contracts;
using Booking.Application.Ports;
using Booking.Domain;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure;

/// <summary>Untracked read adapter with organizer scope and six zero-filled calendar-month buckets.</summary>
public sealed class BookingQueries(BookingDbContext db) : IBookingQueries
{
    /// <summary>Reads only the caller's bookings, newest first with a stable id tie-breaker.</summary>
    public async Task<IReadOnlyList<BookingDto>> GetMineAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await Project(db.Bookings.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Reads the Admin list, optionally restricting results to one lifecycle state.</summary>
    public async Task<IReadOnlyList<BookingDto>> ListAsync(
        string? status,
        CancellationToken cancellationToken)
    {
        var query = db.Bookings.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        return await Project(query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Counts confirmed sales, cancelled purchases, top ten events, and revenue for the last six months.</summary>
    public async Task<StatsDto> GetStatsAsync(
        Guid? organizerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var query = db.Bookings.AsNoTracking();
        if (organizerId is { } owner)
        {
            query = query.Where(x => x.OrganizerId == owner);
        }

        // Read only the columns required for aggregation, not tracked aggregates or cross-service data.
        var rows = await query.Select(x => new SalesRow(
            x.EventId, x.EventTitle, x.Quantity, x.Total, x.Status, x.CreatedAt))
            .ToListAsync(cancellationToken);
        // Keep cancelled bookings in history, but do not count them as tickets sold or money earned.
        var confirmed = rows.Where(x => x.Status == BookingStatus.Confirmed).ToList();
        var totals = new BookingTotals(
            confirmed.Sum(x => x.Total),
            confirmed.Sum(x => x.Quantity),
            rows.Count,
            rows.Count(x => x.Status == BookingStatus.Cancelled));

        // Show this month and the previous five, including months with no sales.
        // A sale at the start of a month belongs to that month, not also to the one before it.
        var firstMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-5);
        var months = Enumerable.Range(0, 6).Select(offset =>
        {
            var start = firstMonth.AddMonths(offset);
            var end = start.AddMonths(1);
            var revenue = confirmed.Where(x => x.CreatedAt >= start && x.CreatedAt < end).Sum(x => x.Total);
            return new MonthlyRevenue(start.ToString("yyyy-MM", CultureInfo.InvariantCulture), revenue);
        }).ToList();

        // Find the ten events with the most tickets sold. A booking for five tickets counts as five, not one.
        var topEvents = confirmed.GroupBy(x => new { x.EventId, x.Title })
            .Select(group => new EventSales(group.Key.EventId, group.Key.Title,
                group.Sum(x => x.Quantity), group.Sum(x => x.Total)))
            .OrderByDescending(x => x.Tickets).ThenBy(x => x.EventId).Take(10).ToList();
        StatusCount[] statusCounts =
        [
            new(BookingStatus.Confirmed, confirmed.Count),
            new(BookingStatus.Cancelled, totals.Cancelled)
        ];

        return new StatsDto(totals, months, topEvents, statusCounts);
    }

    /// <summary>Projects only public booking fields; the SQL read never exposes a tracked entity.</summary>
    private static IQueryable<BookingDto> Project(IQueryable<Domain.Booking> query)
    {
        return query.Select(x => new BookingDto(
            x.Id, x.UserId, x.EventId, x.EventTitle, x.EventStartsAt, x.OrganizerId,
            x.Quantity, x.UnitPrice, x.Total, x.Status, x.PaymentRef, x.ReservationId,
            x.SeatReleasePending, x.CreatedAt));
    }

    // Narrow internal read shape used only while calculating dashboard aggregates.
    private sealed record SalesRow(
        int EventId,
        string Title,
        int Quantity,
        decimal Total,
        string Status,
        DateTimeOffset CreatedAt);
}
