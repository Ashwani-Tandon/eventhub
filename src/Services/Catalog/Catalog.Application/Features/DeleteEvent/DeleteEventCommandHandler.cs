// Deletes an owned event only when no seats are booked.
// Authorization is checked in the handler and the deletion invariant remains in Domain.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.DeleteEvent;

/// <summary>
/// Checks owner or Admin access and refuses booked events before saving the deletion.
/// </summary>
public sealed class DeleteEventCommandHandler(
    IEventRepository events,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<DeleteEventCommand>
{
    /// <summary>
    /// Checks owner or Admin access and refuses booked events before saving the deletion.
    /// </summary>
    public async Task<Result> Handle(
        DeleteEventCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure(EventErrors.Unauthenticated);
        }

        var eventItem = await events.FindAsync(request.Id, cancellationToken);
        if (eventItem is null)
        {
            return Result.Failure(EventErrors.NotFound);
        }

        if (eventItem.OrganizerId != userId && currentUser.Role != ApplicationRoles.Admin)
        {
            return Result.Failure(EventErrors.Forbidden);
        }

        // Owning the event is not enough to delete it: refuse while customers still have booked seats.
        var deletion = eventItem.CanBeDeleted();
        if (deletion.IsFailure)
        {
            return deletion;
        }

        events.Remove(eventItem);
        await unitOfWork.SaveAsync(cancellationToken);
        return Result.Success();
    }
}
