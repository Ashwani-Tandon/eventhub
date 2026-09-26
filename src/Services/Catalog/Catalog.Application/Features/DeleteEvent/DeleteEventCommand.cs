// Deletes an owned event only when no seats are booked.
// Authorization is checked in the handler and the deletion invariant remains in Domain.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.DeleteEvent;

/// <summary>
/// Identifies the event the caller wants to delete.
/// </summary>
public sealed record DeleteEventCommand(int Id) : ICommand;
