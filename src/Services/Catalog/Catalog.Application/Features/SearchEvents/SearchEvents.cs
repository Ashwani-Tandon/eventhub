// Represents public filtered and paged event discovery.
// The query handler delegates projection to the read port and never loads tracked aggregates.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using FluentValidation;

namespace Catalog.Application.Features.SearchEvents;

public sealed record SearchEventsQuery(string? Search, string? Category, string? City,
    DateTimeOffset? From, DateTimeOffset? To, decimal? MaxPrice, int Page = 1, int PageSize = 12)
    : IQuery<EventSearchResult>;

public sealed class SearchEventsQueryValidator : AbstractValidator<SearchEventsQuery>
{
    public SearchEventsQueryValidator()
    {
        RuleFor(x => x.Category).Must(x => x is null || Categories.IsKnown(x)).WithMessage("Choose a known category.");
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).When(x => x.From.HasValue && x.To.HasValue);
    }
}

public sealed class SearchEventsQueryHandler(IEventQueries queries, TimeProvider clock)
    : IQueryHandler<SearchEventsQuery, EventSearchResult>
{
    public async Task<Result<EventSearchResult>> Handle(SearchEventsQuery request, CancellationToken cancellationToken) =>
        Result<EventSearchResult>.Success(await queries.SearchAsync(new(request.Search, request.Category,
            request.City, request.From, request.To, request.MaxPrice, request.Page, request.PageSize,
            clock.GetUtcNow()), cancellationToken));
}
