// Deletes an owned event only when no seats are booked.
// Authorization is checked in the handler and the deletion invariant remains in Domain.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.DeleteEvent;

/// <summary>
/// Deletes an owned event only when no seats are booked. Authorization is checked in the handler and the deletion invariant remains in Domain.
/// </summary>
public sealed class DeleteEventCommandValidator : AbstractValidator<DeleteEventCommand>
{
    /// <summary>
    /// Rejects invalid event identifiers before loading an event for deletion.
    /// </summary>
    public DeleteEventCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
