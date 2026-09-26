// Fetches one public event DTO by identifier.
// The read port projects directly from SQL and the handler turns a missing row into a typed result.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using FluentValidation;

namespace Catalog.Application.Features.GetEventById;

/// <summary>
/// Reads a public event DTO and returns NotFound when its identifier does not exist.
/// </summary>
public sealed class GetEventByIdQueryHandler(IEventQueries queries) : IQueryHandler<GetEventByIdQuery, EventDto>
{
    /// <summary>
    /// Reads a public event DTO and returns NotFound when its identifier does not exist.
    /// </summary>
    public async Task<Result<EventDto>> Handle(
        GetEventByIdQuery request,
        CancellationToken cancellationToken)
    {
        var eventItem = await queries.GetAsync(request.Id, cancellationToken);
        return eventItem is null ? Result<EventDto>.Failure(EventErrors.NotFound) : Result<EventDto>.Success(eventItem);
    }
}
