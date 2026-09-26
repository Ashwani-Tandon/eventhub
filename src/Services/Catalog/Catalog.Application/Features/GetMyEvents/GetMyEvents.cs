// Lists events owned by the current organizer, while administrators can see all.
// Identity comes only from validated JWT claims exposed through ICurrentUser.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.GetMyEvents;

public sealed record GetMyEventsQuery : IQuery<IReadOnlyList<EventDto>>;
public sealed class GetMyEventsQueryValidator : AbstractValidator<GetMyEventsQuery>;
public sealed class GetMyEventsQueryHandler(IEventQueries queries, ICurrentUser currentUser)
    : IQueryHandler<GetMyEventsQuery, IReadOnlyList<EventDto>>
{
    public async Task<Result<IReadOnlyList<EventDto>>> Handle(GetMyEventsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return Result<IReadOnlyList<EventDto>>.Failure(EventErrors.Unauthenticated);
        var events = await queries.GetMineAsync(userId, currentUser.Role == ApplicationRoles.Admin, cancellationToken);
        return Result<IReadOnlyList<EventDto>>.Success(events);
    }
}
