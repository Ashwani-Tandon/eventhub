// Carries event creation through validation into the domain aggregate.
// The handler assigns ownership from the authenticated user rather than trusting request data.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.CreateEvent;

public sealed record CreateEventCommand(string Title, string Description, string Category, string Venue,
    string City, DateTimeOffset StartsAt, decimal Price, int Capacity) : ICommand<EventDto>;

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator(TimeProvider clock)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Category).Must(Categories.IsKnown).WithMessage("Choose a known category.");
        RuleFor(x => x.StartsAt).GreaterThan(_ => clock.GetUtcNow()).WithMessage("Start time must be in the future.");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 10000);
    }
}

public sealed class CreateEventCommandHandler(IEventRepository events, IUnitOfWork unitOfWork,
    ICurrentUser currentUser, TimeProvider clock) : ICommandHandler<CreateEventCommand, EventDto>
{
    public async Task<Result<EventDto>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId) return Result<EventDto>.Failure(EventErrors.Unauthenticated);
        var created = Event.Create(request.Title, request.Description, request.Category, request.Venue,
            request.City, request.StartsAt, request.Price, request.Capacity, userId, clock.GetUtcNow());
        if (created.IsFailure) return Result<EventDto>.Failure(created.Error!);
        events.Add(created.Value);
        await unitOfWork.SaveAsync(cancellationToken);
        return Result<EventDto>.Success(EventMappings.ToDto(created.Value));
    }
}
