// Lists events owned by the current organizer, while administrators can see all.
// Identity comes only from validated JWT claims exposed through ICurrentUser.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.GetMyEvents;

/// <summary>
/// Lists events owned by the current organizer, while administrators can see all. Identity comes only from validated JWT claims exposed through ICurrentUser.
/// </summary>
public sealed class GetMyEventsQueryValidator : AbstractValidator<GetMyEventsQuery>;
