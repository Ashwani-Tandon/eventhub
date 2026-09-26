// Fetches one public event DTO by identifier.
// The read port projects directly from SQL and the handler turns a missing row into a typed result.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using FluentValidation;

namespace Catalog.Application.Features.GetEventById;

/// <summary>
/// Fetches one public event DTO by identifier. The read port projects directly from SQL and the handler turns a missing row into a typed result.
/// </summary>
public sealed class GetEventByIdQueryValidator : AbstractValidator<GetEventByIdQuery>
{
    /// <summary>
    /// Rejects invalid event identifiers before querying Catalog.
    /// </summary>
    public GetEventByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
