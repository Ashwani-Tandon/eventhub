// Updates an existing event after checking ownership and domain invariants.
// Its validator handles request shape while the aggregate protects booked capacity and changed dates.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.UpdateEvent;

/// <summary>
/// Carries the route id and replacement event fields to the update handler.
/// </summary>
public sealed record UpdateEventCommand(
    int Id,
    string Title,
    string Description,
    string Category,
    string Venue,
    string City,
    DateTimeOffset StartsAt,
    decimal Price,
    int Capacity,
    string RowVersion) : ICommand<EventDto>;
