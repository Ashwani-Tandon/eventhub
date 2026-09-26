// Updates an existing event after checking ownership and domain invariants.
// Its validator handles request shape while the aggregate protects booked capacity and changed dates.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.UpdateEvent;

public sealed record UpdateEventCommand(int Id, string Title, string Description, string Category,
    string Venue, string City, DateTimeOffset StartsAt, decimal Price, int Capacity) : ICommand<EventDto>;

public sealed class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Category).Must(Categories.IsKnown).WithMessage("Choose a known category.");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 10000);
    }
}

public sealed class UpdateEventCommandHandler(IEventRepository events, IUnitOfWork unitOfWork,
    ICurrentUser currentUser, TimeProvider clock) : ICommandHandler<UpdateEventCommand, EventDto>
{
    public async Task<Result<EventDto>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId) return Result<EventDto>.Failure(EventErrors.Unauthenticated);
        var eventItem = await events.FindAsync(request.Id, cancellationToken);
        if (eventItem is null) return Result<EventDto>.Failure(EventErrors.NotFound);
        if (eventItem.OrganizerId != userId && currentUser.Role != ApplicationRoles.Admin)
            return Result<EventDto>.Failure(EventErrors.Forbidden);

        var update = eventItem.Update(request.Title, request.Description, request.Category, request.Venue,
            request.City, request.StartsAt, request.Price, request.Capacity, clock.GetUtcNow());
        if (update.IsFailure) return Result<EventDto>.Failure(update.Error!);
        await unitOfWork.SaveAsync(cancellationToken);
        return Result<EventDto>.Success(EventMappings.ToDto(eventItem));
    }
}
