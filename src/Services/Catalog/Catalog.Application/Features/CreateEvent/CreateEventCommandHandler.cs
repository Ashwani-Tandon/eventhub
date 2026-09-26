// Carries event creation through validation into the domain aggregate.
// The handler assigns ownership from the authenticated user rather than trusting request data.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.CreateEvent;

/// <summary>
/// Creates an event owned by the current user, saves it, and returns its DTO.
/// </summary>
public sealed class CreateEventCommandHandler(
    IEventRepository events,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : ICommandHandler<CreateEventCommand, EventDto>
{
    /// <summary>
    /// Creates an event owned by the current user, saves it, and returns its DTO.
    /// </summary>
    public async Task<Result<EventDto>> Handle(
        CreateEventCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<EventDto>.Failure(EventErrors.Unauthenticated);
        }

        var created = Event.Create(request.Title, request.Description, request.Category, request.Venue,
            request.City, request.StartsAt, request.Price, request.Capacity, userId, clock.GetUtcNow());
        if (created.IsFailure)
        {
            return Result<EventDto>.Failure(created.Error!);
        }

        events.Add(created.Value);
        await unitOfWork.SaveAsync(cancellationToken);
        return Result<EventDto>.Success(EventMappings.ToDto(created.Value));
    }
}
