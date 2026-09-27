// Scopes sales statistics to the caller's organizer id, with an Admin override.
// Stored event snapshots make this handler independent of Catalog availability.
using Booking.Application.Contracts;
using Booking.Application.Ports;
using Booking.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;

namespace Booking.Application.Features.GetBookingStats;

/// <summary>Uses the signed role to choose all organizers or only the current organizer.</summary>
public sealed class GetBookingStatsQueryHandler(
    IBookingQueries queries,
    ICurrentUser currentUser,
    TimeProvider clock) : IQueryHandler<GetBookingStatsQuery, StatsDto>
{
    /// <summary>Reads the requested projection from Booking's database and wraps it in a successful Result.</summary>
    public async Task<Result<StatsDto>> Handle(
        GetBookingStatsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<StatsDto>.Failure(BookingErrors.Unauthenticated);
        }

        // Admin sees sales for all events. An organizer sees sales only for events they own.
        Guid? organizerId = currentUser.Role == BookingRoles.Admin ? null : userId;
        var stats = await queries.GetStatsAsync(organizerId, clock.GetUtcNow(), cancellationToken);
        return Result<StatsDto>.Success(stats);
    }
}
