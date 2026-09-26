// Fetches one public event DTO by identifier.
// The read port projects directly from SQL and the handler turns a missing row into a typed result.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using FluentValidation;

namespace Catalog.Application.Features.GetEventById;

public sealed record GetEventByIdQuery(int Id) : IQuery<EventDto>;
public sealed class GetEventByIdQueryValidator : AbstractValidator<GetEventByIdQuery>
{
    public GetEventByIdQueryValidator() => RuleFor(x => x.Id).GreaterThan(0);
}
public sealed class GetEventByIdQueryHandler(IEventQueries queries) : IQueryHandler<GetEventByIdQuery, EventDto>
{
    public async Task<Result<EventDto>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var eventItem = await queries.GetAsync(request.Id, cancellationToken);
        return eventItem is null ? Result<EventDto>.Failure(EventErrors.NotFound) : Result<EventDto>.Success(eventItem);
    }
}
