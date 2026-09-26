// Defines only the three Catalog operations needed by Booking's write use cases.
// The HTTP adapter handles discovery, user-token forwarding, credentials, and dependency failures.
using EventHub.BuildingBlocks.Results;

namespace Booking.Application.Ports;

/// <summary>Catalog event snapshot used to validate a purchase and copy its independent read facts.</summary>
public sealed record CatalogEvent(
    int Id,
    string Title,
    DateTimeOffset StartsAt,
    Guid OrganizerId,
    decimal Price,
    int SeatsLeft);

/// <summary>Narrow dependency boundary for fetching an event, reserving, and compensating or cancelling.</summary>
public interface ICatalogClient
{
    /// <summary>Reads the event facts required before any reservation or payment takes place.</summary>
    Task<Result<CatalogEvent>> GetEventAsync(
        int eventId,
        CancellationToken cancellationToken);

    /// <summary>Holds the requested quantity under a stable reservation identity.</summary>
    Task<Result> ReserveAsync(
        int eventId,
        Guid reservationId,
        int quantity,
        CancellationToken cancellationToken);

    /// <summary>Returns held seats once, using the same identity as the original reservation.</summary>
    Task<Result> ReleaseAsync(
        Guid reservationId,
        CancellationToken cancellationToken);
}
