// Lists events owned by the current organizer, while administrators can see all.
// Identity comes only from validated JWT claims exposed through ICurrentUser.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.GetMyEvents;

/// <summary>
/// Reads the current organizer's events, including past ones, or all events for an Admin.
/// </summary>
public sealed class GetMyEventsQueryHandler(
    IEventQueries queries,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyEventsQuery, IReadOnlyList<EventDto>>
{
    /// <summary>
    /// Reads the current organizer's events, including past ones, or all events for an Admin.
    /// </summary>
    public async Task<Result<IReadOnlyList<EventDto>>> Handle(
        GetMyEventsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<EventDto>>.Failure(EventErrors.Unauthenticated);
        }

        var events = await queries.GetMineAsync(userId, currentUser.Role == ApplicationRoles.Admin, cancellationToken);
        return Result<IReadOnlyList<EventDto>>.Success(events);
    }
}
