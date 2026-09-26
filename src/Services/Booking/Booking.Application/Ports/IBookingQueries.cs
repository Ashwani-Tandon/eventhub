// Defines DTO reads and aggregates from Booking's own database.
// These methods never need Catalog because each purchase stores its event and organizer snapshot.
using Booking.Application.Contracts;

namespace Booking.Application.Ports;

/// <summary>Read-only projections for attendee history, Admin listing, and scoped organizer statistics.</summary>
public interface IBookingQueries
{
    /// <summary>Lists the user's purchases newest first, including completed and pending cancellations.</summary>
    Task<IReadOnlyList<BookingDto>> GetMineAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>Lists all purchases with an optional lifecycle-state filter.</summary>
    Task<IReadOnlyList<BookingDto>> ListAsync(
        string? status,
        CancellationToken cancellationToken);

    /// <summary>Aggregates only the selected organizer, or everyone when organizerId is null.</summary>
    Task<StatsDto> GetStatsAsync(
        Guid? organizerId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
