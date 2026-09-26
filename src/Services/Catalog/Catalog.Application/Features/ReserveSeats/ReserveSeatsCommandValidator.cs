// Requests an atomic, idempotent seat hold using a caller-supplied reservation id.
// Infrastructure performs the concurrency-sensitive transaction behind the repository port.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.ReserveSeats;

/// <summary>
/// Requests an atomic, idempotent seat hold using a caller-supplied reservation id. Infrastructure performs the concurrency-sensitive transaction behind the repository port.
/// </summary>
public sealed class ReserveSeatsCommandValidator : AbstractValidator<ReserveSeatsCommand>
{
    /// <summary>
    /// Requires an event id, a nonempty reservation GUID, and a positive quantity before any seats can change.
    /// </summary>
    public ReserveSeatsCommandValidator()
    {
        RuleFor(x => x.EventId).GreaterThan(0);
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
