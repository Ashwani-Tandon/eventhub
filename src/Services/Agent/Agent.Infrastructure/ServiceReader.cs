// Reads JSON from a service after its shared retry/timeout pipeline has run.
// Expected outages and HTTP rejections become Results, allowing tools to explain failures instead of crashing chat.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventHub.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;
namespace Agent.Infrastructure;

internal static class ServiceReader
{
    private static readonly Action<ILogger, string, Exception?> LogUnavailable = LoggerMessage.Define<string>(
        LogLevel.Warning, new EventId(9010, nameof(LogUnavailable)), "Agent could not read {ServiceName}");

    /// <summary>Gets only the local projection; extra fields in the service's JSON are ignored by the serializer.</summary>
    public static async Task<Result<T>> ReadAsync<T>(HttpClient client, string path, string serviceName,
        ILogger logger, CancellationToken cancellationToken)
    {
        var unavailable = Error.Unavailable($"Agent.{serviceName}Unavailable", $"{serviceName} is temporarily unavailable");
        try
        {
            using var response = await client.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // Preserve permission and missing-record outcomes. Retried 5xx/429 failures mean the dependency is unavailable.
                var error = response.StatusCode switch
                {
                    HttpStatusCode.NotFound => Error.NotFound("Agent.NotFound", "Not found"),
                    HttpStatusCode.Forbidden => Error.Forbidden("Agent.Forbidden", "You don't have permission"),
                    HttpStatusCode.Unauthorized => Error.Unauthorized("Agent.Unauthorized", "Please sign in again"),
                    HttpStatusCode.BadRequest => Error.Validation("Agent.InvalidFilters", "Invalid event filters. Check the category, price and date range."),
                    _ => unavailable
                };
                return Result<T>.Failure(error);
            }
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return value is null ? Result<T>.Failure(unavailable) : Result<T>.Success(value);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or BrokenCircuitException or TimeoutRejectedException
            || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // Request cancellation is allowed to travel upward; only dependency faults are translated here.
            LogUnavailable(logger, serviceName, exception);
            return Result<T>.Failure(unavailable);
        }
    }
}
