// Fetches one public event DTO by identifier.
// The read port projects directly from SQL and the handler turns a missing row into a typed result.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using FluentValidation;

namespace Catalog.Application.Features.GetEventById;

/// <summary>
/// Identifies the public event to read.
/// </summary>
public sealed record GetEventByIdQuery(int Id) : IQuery<EventDto>;
