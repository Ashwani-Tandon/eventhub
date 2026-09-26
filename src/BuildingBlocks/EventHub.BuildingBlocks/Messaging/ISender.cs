using EventHub.BuildingBlocks.Results;

namespace EventHub.BuildingBlocks.Messaging;

public interface ISender
{
    Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
        where TResponse : IResult<TResponse>;
}
// Provides the endpoint's entry point into the mediator.
// Send routes a request through shared behaviors to its matching handler.
