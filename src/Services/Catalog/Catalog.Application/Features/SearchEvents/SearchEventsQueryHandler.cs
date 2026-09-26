// Represents public filtered and paged event discovery.
// The query handler delegates projection to the read port and never loads tracked aggregates.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using FluentValidation;

namespace Catalog.Application.Features.SearchEvents;

/// <summary>
/// Reads a filtered event page and total count using the current time for upcoming-event defaults.
/// </summary>
public sealed class SearchEventsQueryHandler(
    IEventQueries queries,
    TimeProvider clock)
    : IQueryHandler<SearchEventsQuery, EventSearchResult>
{
    /// <summary>
    /// Reads a filtered event page and total count using the current time for upcoming-event defaults.
    /// </summary>
    public async Task<Result<EventSearchResult>> Handle(
        SearchEventsQuery request,
        CancellationToken cancellationToken)
    {
        return Result<EventSearchResult>.Success(await queries.SearchAsync(new(request.Search, request.Category,
            request.City, request.From, request.To, request.MaxPrice, request.Page, request.PageSize,
            clock.GetUtcNow()), cancellationToken));
    }
}
