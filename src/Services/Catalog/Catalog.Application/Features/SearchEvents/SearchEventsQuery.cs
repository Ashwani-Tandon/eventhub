// Represents public filtered and paged event discovery.
// The query handler delegates projection to the read port and never loads tracked aggregates.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using FluentValidation;

namespace Catalog.Application.Features.SearchEvents;

/// <summary>
/// Carries public filters and paging values without changing stored events.
/// </summary>
public sealed record SearchEventsQuery(
    string? Search,
    string? Category,
    string? City,
    DateTimeOffset? From,
    DateTimeOffset? To,
    decimal? MaxPrice,
    int Page = 1,
    int PageSize = 12)
    : IQuery<EventSearchResult>;
