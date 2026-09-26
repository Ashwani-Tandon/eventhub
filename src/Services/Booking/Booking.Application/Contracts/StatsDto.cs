// Defines the dashboard data computed entirely from Booking's stored event snapshots.
// Revenue excludes cancelled purchases and month buckets include months with no sales.
namespace Booking.Application.Contracts;

/// <summary>Dashboard totals, six revenue months, top events, and both status counts.</summary>
public sealed record StatsDto(
    BookingTotals Totals,
    IReadOnlyList<MonthlyRevenue> RevenueByMonth,
    IReadOnlyList<EventSales> TopEvents,
    IReadOnlyList<StatusCount> StatusCounts);

/// <summary>Confirmed revenue and tickets, total booking count, and cancelled booking count.</summary>
public sealed record BookingTotals(
    decimal Revenue,
    int TicketsSold,
    int Bookings,
    int Cancelled);

/// <summary>A YYYY-MM revenue bucket, including zero when that month has no confirmed purchases.</summary>
public sealed record MonthlyRevenue(
    string Month,
    decimal Revenue);

/// <summary>Confirmed ticket and revenue totals for an event's copied title and identifier.</summary>
public sealed record EventSales(
    int EventId,
    string Title,
    int Tickets,
    decimal Revenue);

/// <summary>Number of bookings in one customer-visible lifecycle state.</summary>
public sealed record StatusCount(
    string Status,
    int Count);
