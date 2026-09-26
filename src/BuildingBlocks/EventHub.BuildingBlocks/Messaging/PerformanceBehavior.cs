using EventHub.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace EventHub.BuildingBlocks.Messaging;

public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    TimeProvider timeProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult<TResponse>
{
    private static readonly TimeSpan SlowRequestThreshold = TimeSpan.FromMilliseconds(500);

    private static readonly Action<ILogger, string, Exception?> LogPipelineEntered =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1200, nameof(LogPipelineEntered)),
            "Mediator pipeline: Performance entered for {RequestName}");

    private static readonly Action<ILogger, string, double, Exception?> LogSlowRequest =
        LoggerMessage.Define<string, double>(
            LogLevel.Warning,
            new EventId(1201, nameof(LogSlowRequest)),
            "Mediator request {RequestName} was slow: {ElapsedMilliseconds} ms");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerContinuation<TResponse> continuation,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = timeProvider.GetTimestamp();

        LogPipelineEntered(logger, requestName, null);

        var response = await continuation(cancellationToken);
        var elapsed = timeProvider.GetElapsedTime(startedAt);

        if (elapsed > SlowRequestThreshold)
        {
            LogSlowRequest(
                logger,
                requestName,
                elapsed.TotalMilliseconds,
                null);
        }

        return response;
    }
}
