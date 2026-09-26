// Reads the Admin list through Booking's own query port.
// Stored event snapshots make this handler independent of Catalog availability.
using Booking.Application.Contracts;
using Booking.Application.Ports;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;

namespace Booking.Application.Features.ListBookings;

/// <summary>Returns filtered safe booking DTOs; the endpoint applies the Admin policy.</summary>
public sealed class ListBookingsQueryHandler(
    IBookingQueries queries) : IQueryHandler<ListBookingsQuery, IReadOnlyList<BookingDto>>
{
    /// <summary>Reads the requested projection from Booking's database and wraps it in a successful Result.</summary>
    public async Task<Result<IReadOnlyList<BookingDto>>> Handle(
        ListBookingsQuery request,
        CancellationToken cancellationToken)
    {
        var bookings = await queries.ListAsync(request.Status, cancellationToken);
        return Result<IReadOnlyList<BookingDto>>.Success(bookings);
    }
}
