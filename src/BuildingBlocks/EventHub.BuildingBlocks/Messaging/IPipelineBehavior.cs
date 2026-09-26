// Defines middleware around a mediator handler.
// Each behavior can call the next operation, inspect its Result, or stop the chain early.
using EventHub.BuildingBlocks.Results;

namespace EventHub.BuildingBlocks.Messaging;

public delegate Task<TResponse> RequestHandlerContinuation<TResponse>(
    CancellationToken cancellationToken)
    where TResponse : IResult<TResponse>;

/// <summary>
/// Defines middleware around a mediator handler. Each behavior can call the next operation, inspect its Result, or stop the chain early.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerContinuation<TResponse> continuation,
        CancellationToken cancellationToken);
}
