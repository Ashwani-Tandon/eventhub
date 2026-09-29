// Describes the small set of event and booking facts the assistant uses for reads and confirmed actions.
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

/// <summary>Sales projections mirror Booking's response without referencing its Application assembly.</summary>
public sealed record SalesFacts(SalesTotals Totals, IReadOnlyList<SalesMonth> RevenueByMonth,
    IReadOnlyList<EventSalesFacts> TopEvents, IReadOnlyList<SalesStatus> StatusCounts);

/// <summary>Caller-scoped totals; revenue and tickets exclude cancelled purchases.</summary>
public sealed record SalesTotals(decimal Revenue, int TicketsSold, int Bookings, int Cancelled);

/// <summary>One month of confirmed revenue in INR.</summary>
public sealed record SalesMonth(string Month, decimal Revenue);

/// <summary>One event's confirmed ticket sales and revenue.</summary>
public sealed record EventSalesFacts(int EventId, string Title, int Tickets, decimal Revenue);

/// <summary>Number of purchases in one booking state.</summary>
public sealed record SalesStatus(string Status, int Count);
