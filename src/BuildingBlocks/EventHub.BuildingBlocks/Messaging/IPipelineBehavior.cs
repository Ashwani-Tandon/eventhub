using EventHub.BuildingBlocks.Results;

namespace EventHub.BuildingBlocks.Messaging;

public delegate Task<TResponse> RequestHandlerContinuation<TResponse>(
    CancellationToken cancellationToken)
    where TResponse : IResult<TResponse>;

public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerContinuation<TResponse> continuation,
        CancellationToken cancellationToken);
}
