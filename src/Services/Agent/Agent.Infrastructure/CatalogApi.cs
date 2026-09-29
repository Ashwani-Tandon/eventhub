// Implements the assistant's event-read port using Catalog's public HTTP endpoints.
// Filters are escaped before sending; discovery and the shared pipeline supply routing, retries and waiting limits.
using System.Globalization;
using Agent.Application.Contracts;
using Agent.Application.Ports;
using EventHub.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;
namespace Agent.Infrastructure;

public sealed class CatalogApi(HttpClient client, ILogger<CatalogApi> logger) : ICatalogApi
{
    /// <summary>Requests one bounded page and retains Total so a larger search is not presented as a complete list.</summary>
    public Task<Result<EventMatches>> SearchAsync(EventFilters filters, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string?>
        {
            ["search"] = filters.Search, ["category"] = filters.Category, ["city"] = filters.City,
            ["maxPrice"] = filters.MaxPrice?.ToString(CultureInfo.InvariantCulture),
            ["from"] = filters.From?.ToString("O", CultureInfo.InvariantCulture),
            ["to"] = filters.To?.ToString("O", CultureInfo.InvariantCulture)
        };
        var parts = new List<string> { "page=1", "pageSize=20" };
        foreach (var (name, value) in values)
        {
            // Omitted filters remain omitted; escaping stops spaces, ampersands and timezone '+' from changing the URL.
            if (!string.IsNullOrWhiteSpace(value)) parts.Add($"{name}={Uri.EscapeDataString(value)}");
        }
        return ServiceReader.ReadAsync<EventMatches>(client, $"/events?{string.Join('&', parts)}", "Catalog", logger, cancellationToken);
    }

    /// <summary>Reads one event through the same authenticated, resilient HTTP boundary.</summary>
    public Task<Result<EventFacts>> GetAsync(int eventId, CancellationToken cancellationToken)
        => ServiceReader.ReadAsync<EventFacts>(client, $"/events/{eventId}", "Catalog", logger, cancellationToken);
}
