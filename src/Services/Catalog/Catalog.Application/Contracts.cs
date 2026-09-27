// Defines Catalog DTOs and narrow persistence/read ports owned by Application.
// Handlers use these contracts while Infrastructure supplies EF Core implementations.
using Catalog.Domain;

namespace Catalog.Application;

/// <summary>
/// Public event fields returned to callers. SeatsLeft is calculated from capacity and booked seats; persistence entities are never exposed.
/// </summary>
public sealed record EventDto(
    int Id,
    string Title,
    string Description,
    string Category,
    string Venue,
    string City,
    DateTimeOffset StartsAt,
    decimal Price,
    int Capacity,
    int SeatsBooked,
    int SeatsLeft,
    Guid OrganizerId,
    string RowVersion);

/// <summary>
/// One page of matching events and the count before paging, so the UI can calculate how many pages exist.
/// </summary>
public sealed record EventSearchResult(
    IReadOnlyList<EventDto> Items,
    int Total);

/// <summary>
/// The stable reservation id and its current Held or Released state, including responses to repeated calls.
/// </summary>
public sealed record ReservationDto(
    Guid ReservationId,
    string Status);

/// <summary>
/// Validated search filters plus the clock snapshot used to exclude past events when no starting date is supplied.
/// </summary>
public sealed record EventSearchCriteria(
    string? Search,
    string? Category,
    string? City,
    DateTimeOffset? From,
    DateTimeOffset? To,
    decimal? MaxPrice,
    int Page,
    int PageSize,
    DateTimeOffset Now);

/// <summary>
/// Defines Catalog DTOs and narrow persistence/read ports owned by Application. Handlers use these contracts while Infrastructure supplies EF Core implementations.
/// </summary>
public static class ApplicationRoles
{
    public const string Admin = "Admin";
}

/// <summary>
/// Defines Catalog DTOs and narrow persistence/read ports owned by Application. Handlers use these contracts while Infrastructure supplies EF Core implementations.
/// </summary>
public static class EventMappings
{
    /// <summary>
    /// Manually maps an aggregate into the public event response.
    /// </summary>
    public static EventDto ToDto(Event eventItem)
    {
        return new(eventItem.Id, eventItem.Title,
        eventItem.Description, eventItem.Category, eventItem.Venue, eventItem.City,
        eventItem.StartsAt, eventItem.Price, eventItem.Capacity, eventItem.SeatsBooked,
        eventItem.Capacity - eventItem.SeatsBooked, eventItem.OrganizerId,
        Convert.ToBase64String(eventItem.RowVersion));
    }
}

/// <summary>
/// Distinguishes a successful hold or replay from a missing event, insufficient seats, or reused id with different inputs.
/// </summary>
public enum ReserveOutcome
{
    Success, EventNotFound, NotEnoughSeats, ReplayMismatch
}

/// <summary>
/// Carries the stored reservation on success and an explicit outcome for expected reservation failures.
/// </summary>
public sealed record ReserveResult(
    ReserveOutcome Outcome,
    SeatReservation? Reservation);

/// <summary>
/// Distinguishes a seat return, an already completed or unknown release, and a reservation owned by another user.
/// </summary>
public enum ReleaseOutcome
{
    Released, NoChange, Forbidden
}

/// <summary>
/// Carries the release outcome without exposing database exceptions to the use-case handler.
/// </summary>
public sealed record ReleaseResult(
    ReleaseOutcome Outcome,
    SeatReservation? Reservation);

public interface IEventRepository
{
    Task<Event?> FindAsync(int id, CancellationToken cancellationToken);
    /// <summary>Saves only if the event has not changed since the organizer opened it; otherwise asks them to reload.</summary>
    Task<bool> SaveUpdateAsync(Event eventItem, byte[] originalRowVersion, CancellationToken cancellationToken);
    void Add(Event eventItem);
    void Remove(Event eventItem);
    Task<ReserveResult> TryReserveAsync(Guid reservationId, int eventId, Guid userId, int quantity,
        DateTimeOffset now, CancellationToken cancellationToken);
}

/// <summary>
/// Defines Catalog DTOs and narrow persistence/read ports owned by Application. Handlers use these contracts while Infrastructure supplies EF Core implementations.
/// </summary>
public interface IReservationRepository
{
    Task<ReleaseResult> ReleaseAsync(Guid reservationId, Guid userId, DateTimeOffset now,
        CancellationToken cancellationToken);
}

/// <summary>
/// Defines Catalog DTOs and narrow persistence/read ports owned by Application. Handlers use these contracts while Infrastructure supplies EF Core implementations.
/// </summary>
public interface IEventQueries
{
    Task<EventSearchResult> SearchAsync(EventSearchCriteria criteria, CancellationToken cancellationToken);
    Task<EventDto?> GetAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<EventDto>> GetMineAsync(Guid userId, bool includeAll,
        CancellationToken cancellationToken);
}

/// <summary>
/// Defines Catalog DTOs and narrow persistence/read ports owned by Application. Handlers use these contracts while Infrastructure supplies EF Core implementations.
/// </summary>
public interface IUnitOfWork
{
    Task SaveAsync(CancellationToken cancellationToken);
}
