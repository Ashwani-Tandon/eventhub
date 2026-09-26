// Describes the current user's booking history read operation.
// The query returns copied booking facts without contacting Catalog.
using Booking.Application.Contracts;
using EventHub.BuildingBlocks.Messaging;

namespace Booking.Application.Features.GetMyBookings;

/// <summary>Requests the caller's bookings newest first.</summary>
public sealed record GetMyBookingsQuery : IQuery<IReadOnlyList<BookingDto>>;
