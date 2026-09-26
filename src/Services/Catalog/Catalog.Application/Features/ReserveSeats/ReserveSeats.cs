// Requests an atomic, idempotent seat hold using a caller-supplied reservation id.
// Infrastructure performs the concurrency-sensitive transaction behind the repository port.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.ReserveSeats;

public sealed record ReserveSeatsCommand(int EventId, Guid ReservationId, int Quantity) : ICommand<ReservationDto>;
public sealed class ReserveSeatsCommandValidator : AbstractValidator<ReserveSeatsCommand>
{
    public ReserveSeatsCommandValidator()
    {
        RuleFor(x => x.EventId).GreaterThan(0);
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class ReserveSeatsCommandHandler(IEventRepository events, ICurrentUser currentUser,
    TimeProvider clock) : ICommandHandler<ReserveSeatsCommand, ReservationDto>
{
    public async Task<Result<ReservationDto>> Handle(ReserveSeatsCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId) return Result<ReservationDto>.Failure(EventErrors.Unauthenticated);
        var result = await events.TryReserveAsync(request.ReservationId, request.EventId, userId,
            request.Quantity, clock.GetUtcNow(), cancellationToken);
        return result.Outcome switch
        {
            ReserveOutcome.Success => Result<ReservationDto>.Success(new(result.Reservation!.Id, result.Reservation.Status)),
            ReserveOutcome.EventNotFound => Result<ReservationDto>.Failure(EventErrors.NotFound),
            ReserveOutcome.NotEnoughSeats => Result<ReservationDto>.Failure(EventErrors.NotEnoughSeats),
            _ => Result<ReservationDto>.Failure(EventErrors.ReplayMismatch)
        };
    }
}
