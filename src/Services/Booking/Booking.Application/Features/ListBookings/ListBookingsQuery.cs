// Describes the Admin booking list and optional status filter read operation.
// The query returns copied booking facts without contacting Catalog.
using Booking.Application.Contracts;
using EventHub.BuildingBlocks.Messaging;

namespace Booking.Application.Features.ListBookings;

/// <summary>Requests all bookings, optionally limited to Confirmed or Cancelled.</summary>
public sealed record ListBookingsQuery(string? Status = null) : IQuery<IReadOnlyList<BookingDto>>;
