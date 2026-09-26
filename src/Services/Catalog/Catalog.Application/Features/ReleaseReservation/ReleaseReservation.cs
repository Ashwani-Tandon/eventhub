// Releases a held reservation idempotently for its authenticated owner.
// The repository makes the state transition and seat return atomic in SQL.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.ReleaseReservation;

public sealed record ReleaseReservationCommand(Guid ReservationId) : ICommand<ReservationDto>;
public sealed class ReleaseReservationCommandValidator : AbstractValidator<ReleaseReservationCommand>
{
    public ReleaseReservationCommandValidator() => RuleFor(x => x.ReservationId).NotEmpty();
}

public sealed class ReleaseReservationCommandHandler(IReservationRepository reservations,
    ICurrentUser currentUser, TimeProvider clock) : ICommandHandler<ReleaseReservationCommand, ReservationDto>
{
    public async Task<Result<ReservationDto>> Handle(ReleaseReservationCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId) return Result<ReservationDto>.Failure(EventErrors.Unauthenticated);
        var result = await reservations.ReleaseAsync(request.ReservationId, userId, clock.GetUtcNow(), cancellationToken);
        if (result.Outcome == ReleaseOutcome.Forbidden)
            return Result<ReservationDto>.Failure(EventErrors.ReservationForbidden);
        return Result<ReservationDto>.Success(new(request.ReservationId, ReservationStatuses.Released));
    }
}
