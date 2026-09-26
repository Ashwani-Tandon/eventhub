// Deletes an owned event only when no seats are booked.
// Authorization is checked in the handler and the deletion invariant remains in Domain.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.DeleteEvent;

public sealed record DeleteEventCommand(int Id) : ICommand;
public sealed class DeleteEventCommandValidator : AbstractValidator<DeleteEventCommand>
{
    public DeleteEventCommandValidator() => RuleFor(x => x.Id).GreaterThan(0);
}

public sealed class DeleteEventCommandHandler(IEventRepository events, IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<DeleteEventCommand>
{
    public async Task<Result> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId) return Result.Failure(EventErrors.Unauthenticated);
        var eventItem = await events.FindAsync(request.Id, cancellationToken);
        if (eventItem is null) return Result.Failure(EventErrors.NotFound);
        if (eventItem.OrganizerId != userId && currentUser.Role != ApplicationRoles.Admin)
            return Result.Failure(EventErrors.Forbidden);
        var deletion = eventItem.CanBeDeleted();
        if (deletion.IsFailure) return deletion;
        events.Remove(eventItem);
        await unitOfWork.SaveAsync(cancellationToken);
        return Result.Success();
    }
}
