// Gives the assistant access to public event reads without knowing HTTP or database details.
// Infrastructure supplies this port by calling Catalog with the current user's login token.
using Agent.Application.Contracts;
using EventHub.BuildingBlocks.Results;
namespace Agent.Application.Ports;

public interface ICatalogApi
{
    /// <summary>Reads up to 20 matching events and the count of all matches.</summary>
    Task<Result<EventMatches>> SearchAsync(EventFilters filters, CancellationToken cancellationToken);
    /// <summary>Reads current facts for one event, or returns a missing-event failure.</summary>
    Task<Result<EventFacts>> GetAsync(int eventId, CancellationToken cancellationToken);
}
