// Logs the request name, elapsed time, and success/failure outcome without logging request contents.
// As the outer behavior, it also observes Results returned early by validation.
using EventHub.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace EventHub.BuildingBlocks.Messaging;

/// <summary>
/// Logs the request name, elapsed time, and success/failure outcome without logging request contents. As the outer behavior, it also observes Results returned early by validation.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    TimeProvider timeProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult<TResponse>
{
    private static readonly Action<ILogger, string, Exception?> LogPipelineEntered =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1000, nameof(LogPipelineEntered)),
            "Mediator pipeline: Logging entered for {RequestName}");

    private static readonly Action<ILogger, string, double, string, Exception?> LogRequestCompleted =
        LoggerMessage.Define<string, double, string>(
            LogLevel.Information,
            new EventId(1001, nameof(LogRequestCompleted)),
            "Mediator request {RequestName} completed in {ElapsedMilliseconds} ms with {Outcome}");

    /// <summary>
    /// Runs the remaining pipeline and logs request name, duration, and success without logging sensitive request contents.
    /// </summary>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerContinuation<TResponse> continuation,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = timeProvider.GetTimestamp();

        LogPipelineEntered(logger, requestName, null);

// Let the request go through its checks and the requested task, then record how long it took.
        // If an input check rejects the request, record that failed result too.
        var response = await continuation(cancellationToken);
        var elapsed = timeProvider.GetElapsedTime(startedAt);

        LogRequestCompleted(
            logger,
            requestName,
            elapsed.TotalMilliseconds,
            response.IsSuccess ? "Success" : "Failure",
            null);

        return response;
    }
}
