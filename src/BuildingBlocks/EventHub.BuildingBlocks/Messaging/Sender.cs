using EventHub.BuildingBlocks.Results;

namespace EventHub.BuildingBlocks.Messaging;

internal sealed class Sender(
    IServiceProvider serviceProvider,
    IEnumerable<IRequestDispatcher> dispatchers) : ISender
{
    private readonly Dictionary<(Type Request, Type Response), IRequestDispatcher> _dispatchers =
        dispatchers.ToDictionary(
            static dispatcher => (dispatcher.RequestType, dispatcher.ResponseType));

    public async Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
        where TResponse : IResult<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_dispatchers.TryGetValue(
                (request.GetType(), typeof(TResponse)),
                out var dispatcher))
        {
            throw new InvalidOperationException(
                $"No request handler is registered for {request.GetType().Name}.");
        }

        var response = await dispatcher.Dispatch(
            request,
            serviceProvider,
            cancellationToken);

        return (TResponse)response;
    }
}
// Finds the dispatch adapter registered for the request and its response type.
// This keeps endpoints independent of concrete handlers and avoids reflection during request dispatch.
