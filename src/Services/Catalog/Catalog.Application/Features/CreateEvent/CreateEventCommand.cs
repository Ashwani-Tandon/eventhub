// Carries event creation through validation into the domain aggregate.
// The handler assigns ownership from the authenticated user rather than trusting request data.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.CreateEvent;

/// <summary>
/// Carries the event fields supplied by the organizer to the creation handler.
/// </summary>
public sealed record CreateEventCommand(
    string Title,
    string Description,
    string Category,
    string Venue,
    string City,
    DateTimeOffset StartsAt,
    decimal Price,
    int Capacity) : ICommand<EventDto>;
