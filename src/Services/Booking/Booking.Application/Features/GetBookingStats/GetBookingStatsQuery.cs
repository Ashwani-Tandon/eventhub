// Describes the organizer-scoped sales dashboard read operation.
// The query returns copied booking facts without contacting Catalog.
using Booking.Application.Contracts;
using EventHub.BuildingBlocks.Messaging;

namespace Booking.Application.Features.GetBookingStats;

/// <summary>Requests sales totals, six month buckets, top events, and status counts.</summary>
public sealed record GetBookingStatsQuery : IQuery<StatsDto>;
