// Releases a held reservation idempotently for its authenticated owner.
// The repository makes the state transition and seat return atomic in SQL.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.ReleaseReservation;

/// <summary>
/// Releases a held reservation idempotently for its authenticated owner. The repository makes the state transition and seat return atomic in SQL.
/// </summary>
public sealed class ReleaseReservationCommandValidator : AbstractValidator<ReleaseReservationCommand>
{
    /// <summary>
    /// Rejects an empty reservation GUID before looking up the reservation.
    /// </summary>
    public ReleaseReservationCommandValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
    }
}
