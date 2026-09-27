// Resolves the typed handler and wraps it with the registered pipeline behaviors.
// Adapters are created at startup; this class runs Logging -> Validation -> Performance -> handler.
using EventHub.BuildingBlocks.Results;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.BuildingBlocks.Messaging;

/// <summary>
/// Resolves the typed handler and wraps it with the registered pipeline behaviors. Adapters are created at startup; this class runs Logging -> Validation -> Performance -> handler.
/// </summary>
internal interface IRequestDispatcher
{
    Type RequestType { get; }

    Type ResponseType { get; }

    Task<object> Dispatch(
        object request,
        IServiceProvider services,
        CancellationToken cancellationToken);
}

/// <summary>
/// Resolves the typed handler and wraps it with the registered pipeline behaviors. Adapters are created at startup; this class runs Logging -> Validation -> Performance -> handler.
/// </summary>
internal sealed class RequestDispatcher<TRequest, TResponse> : IRequestDispatcher
    where TRequest : IRequest<TResponse>
    where TResponse : IResult<TResponse>
{
    public Type RequestType => typeof(TRequest);

    public Type ResponseType => typeof(TResponse);

    /// <summary>
    /// Resolves the typed handler and wraps it from inside out so behaviors execute in registration order.
    /// </summary>
    public async Task<object> Dispatch(
        object request,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var handler = services.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = services
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .ToArray();

        // Start with the handler, then wrap from the inside out. Reversing registration
        // order makes Logging the outermost behavior and therefore the first to execute.
        RequestHandlerContinuation<TResponse> next =
            token => handler.Handle(typedRequest, token);

        foreach (var behavior in behaviors)
        {
// Remember the next step before adding this check in front of it.
            // Each check must move to the next step, rather than accidentally calling itself forever.
            var currentNext = next;
            next = token => behavior.Handle(typedRequest, currentNext, token);
        }

// Start the checks in order. Bad input stops here before the app performs the user's task.
        return await next(cancellationToken);
    }
}
