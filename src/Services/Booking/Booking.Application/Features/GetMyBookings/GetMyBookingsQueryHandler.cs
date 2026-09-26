// Reads booking history for the authenticated user.
// Stored event snapshots make this handler independent of Catalog availability.
using Booking.Application.Contracts;
using Booking.Application.Ports;
using Booking.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;

namespace Booking.Application.Features.GetMyBookings;

/// <summary>Selects only the caller's bookings, including cancelled bookings and pending releases.</summary>
public sealed class GetMyBookingsQueryHandler(
    IBookingQueries queries,
    ICurrentUser currentUser) : IQueryHandler<GetMyBookingsQuery, IReadOnlyList<BookingDto>>
{
    /// <summary>Reads the requested projection from Booking's database and wraps it in a successful Result.</summary>
    public async Task<Result<IReadOnlyList<BookingDto>>> Handle(
        GetMyBookingsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<BookingDto>>.Failure(BookingErrors.Unauthenticated);
        }

        var bookings = await queries.GetMineAsync(userId, cancellationToken);
        return Result<IReadOnlyList<BookingDto>>.Success(bookings);
    }
}
