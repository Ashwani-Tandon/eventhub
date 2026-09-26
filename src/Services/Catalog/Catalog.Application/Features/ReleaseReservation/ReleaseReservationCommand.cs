// Releases a held reservation idempotently for its authenticated owner.
// The repository makes the state transition and seat return atomic in SQL.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.ReleaseReservation;

/// <summary>
/// Identifies the reservation whose seats Booking wants to return.
/// </summary>
public sealed record ReleaseReservationCommand(Guid ReservationId) : ICommand<ReservationDto>;
