// Defines Catalog DTOs and narrow persistence/read ports owned by Application.
// Handlers use these contracts while Infrastructure supplies EF Core implementations.
using Catalog.Domain;

namespace Catalog.Application;

public sealed record EventDto(int Id, string Title, string Description, string Category, string Venue,
    string City, DateTimeOffset StartsAt, decimal Price, int Capacity, int SeatsBooked, int SeatsLeft,
    Guid OrganizerId);
public sealed record EventSearchResult(IReadOnlyList<EventDto> Items, int Total);
public sealed record ReservationDto(Guid ReservationId, string Status);
public sealed record EventSearchCriteria(string? Search, string? Category, string? City,
    DateTimeOffset? From, DateTimeOffset? To, decimal? MaxPrice, int Page, int PageSize,
    DateTimeOffset Now);

public static class ApplicationRoles
{
    public const string Admin = "Admin";
}

public static class EventMappings
{
    public static EventDto ToDto(Event eventItem) => new(eventItem.Id, eventItem.Title,
        eventItem.Description, eventItem.Category, eventItem.Venue, eventItem.City,
        eventItem.StartsAt, eventItem.Price, eventItem.Capacity, eventItem.SeatsBooked,
        eventItem.Capacity - eventItem.SeatsBooked, eventItem.OrganizerId);
}

public enum ReserveOutcome { Success, EventNotFound, NotEnoughSeats, ReplayMismatch }
public sealed record ReserveResult(ReserveOutcome Outcome, SeatReservation? Reservation);
public enum ReleaseOutcome { Released, NoChange, Forbidden }
public sealed record ReleaseResult(ReleaseOutcome Outcome, SeatReservation? Reservation);

public interface IEventRepository
{
    Task<Event?> FindAsync(int id, CancellationToken cancellationToken);
    void Add(Event eventItem);
    void Remove(Event eventItem);
    Task<ReserveResult> TryReserveAsync(Guid reservationId, int eventId, Guid userId, int quantity,
        DateTimeOffset now, CancellationToken cancellationToken);
}

public interface IReservationRepository
{
    Task<ReleaseResult> ReleaseAsync(Guid reservationId, Guid userId, DateTimeOffset now,
        CancellationToken cancellationToken);
}

public interface IEventQueries
{
    Task<EventSearchResult> SearchAsync(EventSearchCriteria criteria, CancellationToken cancellationToken);
    Task<EventDto?> GetAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<EventDto>> GetMineAsync(Guid userId, bool includeAll,
        CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task SaveAsync(CancellationToken cancellationToken);
}
