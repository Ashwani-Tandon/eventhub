// Represents public filtered and paged event discovery.
// The query handler delegates projection to the read port and never loads tracked aggregates.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using FluentValidation;

namespace Catalog.Application.Features.SearchEvents;

/// <summary>
/// Represents public filtered and paged event discovery. The query handler delegates projection to the read port and never loads tracked aggregates.
/// </summary>
public sealed class SearchEventsQueryValidator : AbstractValidator<SearchEventsQuery>
{
    /// <summary>
    /// Rejects unknown categories, negative price limits, invalid paging, and a date range whose end precedes its start.
    /// </summary>
    public SearchEventsQueryValidator()
    {
        RuleFor(x => x.Category).Must(x => x is null || Categories.IsKnown(x)).WithMessage("Choose a known category.");
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).When(x => x.From.HasValue && x.To.HasValue);
    }
}
