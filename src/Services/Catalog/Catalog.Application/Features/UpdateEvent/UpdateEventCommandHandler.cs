// Updates an existing event after checking ownership and domain invariants.
// Its validator handles request shape while the aggregate protects booked capacity and changed dates.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.UpdateEvent;

/// <summary>
/// Checks owner or Admin access, applies domain update rules, saves, and returns the updated event.
/// </summary>
public sealed class UpdateEventCommandHandler(
    IEventRepository events,
    ICurrentUser currentUser,
    TimeProvider clock) : ICommandHandler<UpdateEventCommand, EventDto>
{
    /// <summary>
    /// Checks owner or Admin access, applies domain update rules, saves, and returns the updated event.
    /// </summary>
    public async Task<Result<EventDto>> Handle(
        UpdateEventCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<EventDto>.Failure(EventErrors.Unauthenticated);
        }

        var eventItem = await events.FindAsync(request.Id, cancellationToken);
        if (eventItem is null)
        {
            return Result<EventDto>.Failure(EventErrors.NotFound);
        }

        if (eventItem.OrganizerId != userId && currentUser.Role != ApplicationRoles.Admin)
        {
            return Result<EventDto>.Failure(EventErrors.Forbidden);
        }

        var update = eventItem.Update(request.Title, request.Description, request.Category, request.Venue,
            request.City, request.StartsAt, request.Price, request.Capacity, clock.GetUtcNow());
        if (update.IsFailure)
        {
            return Result<EventDto>.Failure(update.Error!);
        }

        var saved = await events.SaveUpdateAsync(eventItem, Convert.FromBase64String(request.RowVersion), cancellationToken);
        if (!saved)
        {
            return Result<EventDto>.Failure(EventErrors.ConcurrencyConflict);
        }

        return Result<EventDto>.Success(EventMappings.ToDto(eventItem));
    }
}
