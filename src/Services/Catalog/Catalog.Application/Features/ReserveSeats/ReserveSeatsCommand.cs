// Requests an atomic, idempotent seat hold using a caller-supplied reservation id.
// Infrastructure performs the concurrency-sensitive transaction behind the repository port.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.ReserveSeats;

/// <summary>
/// Carries the event id, stable reservation id, and quantity supplied by Booking.
/// </summary>
public sealed record ReserveSeatsCommand(
    int EventId,
    Guid ReservationId,
    int Quantity) : ICommand<ReservationDto>;
