// Provides the endpoint's entry point into the mediator.
// Send routes a request through shared behaviors to its matching handler.
using EventHub.BuildingBlocks.Results;

namespace EventHub.BuildingBlocks.Messaging;

/// <summary>
/// Provides the endpoint's entry point into the mediator. Send routes a request through shared behaviors to its matching handler.
/// </summary>
public interface ISender
{
    Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
        where TResponse : IResult<TResponse>;
}
