// Describes the small set of event and booking facts the assistant can read.
// Each service still owns its HTTP contract; these local projections contain only facts needed for answers.
namespace Agent.Application.Contracts;

/// <summary>Catalog facts used in search and event-detail answers; all prices are INR.</summary>
public sealed record EventFacts(int Id, string Title, string Description, string Category,
    string Venue, string City, DateTimeOffset StartsAt, decimal Price, int SeatsLeft);

/// <summary>One matching page plus the full match count, so the assistant does not claim a partial list is complete.</summary>
public sealed record EventMatches(IReadOnlyList<EventFacts> Items, int Total);

/// <summary>Booking snapshots belong to the authenticated caller; no user identity is accepted from the model.</summary>
public sealed record BookingFacts(int Id, int EventId, string EventTitle, DateTimeOffset EventStartsAt,
    int Quantity, decimal Total, string Status, bool SeatReleasePending);

/// <summary>Optional filters passed to Catalog; omitted starting dates use Catalog's upcoming-event rule.</summary>
public sealed record EventFilters(string? Search, string? Category, string? City,
    decimal? MaxPrice, DateTimeOffset? From, DateTimeOffset? To);
