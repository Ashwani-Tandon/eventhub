// Builds the shared service-to-service timeout, retry, and circuit-breaker pipeline.
// Typed clients opt in here so safe retries and trace-correlated transition logs stay consistent.
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace EventHub.ServiceDefaults;

/// <summary>Shared HTTP resilience registration and request-idempotency markers.</summary>
public static class EventHubResilienceExtensions
{
    private const string PipelineName = "eventhub";
    private const string IdempotencyHeaderName = "Idempotency-Key";
    private static readonly HttpRequestOptionsKey<bool> IdempotentRequestKey =
        new("EventHub.Resilience.IdempotentRequest");
    private static readonly ResiliencePropertyKey<string> TraceIdKey =
        new("EventHub.Resilience.TraceId");
    private static readonly Action<ILogger, string, int, double, string, Exception?> RetryLog =
        LoggerMessage.Define<string, int, double, string>(
            LogLevel.Warning,
            new EventId(7101, nameof(RetryLog)),
            "Retrying {DependencyName} call; retry {RetryAttempt} after {RetryDelayMs} ms; TraceId {TraceId}");
    private static readonly Action<ILogger, string, string, double, string, Exception?> CircuitLog =
        LoggerMessage.Define<string, string, double, string>(
            LogLevel.Warning,
            new EventId(7102, nameof(CircuitLog)),
            "Circuit for {DependencyName} changed to {CircuitState}; break duration {BreakDurationSeconds} s; TraceId {TraceId}");
    private static readonly Action<ILogger, string, string, double, string, Exception?> TimeoutLog =
        LoggerMessage.Define<string, string, double, string>(
            LogLevel.Warning,
            new EventId(7103, nameof(TimeoutLog)),
            "{TimeoutKind} timeout calling {DependencyName} after {TimeoutSeconds} s; TraceId {TraceId}");

    /// <summary>Adds the configured outer total timeout, retry, circuit breaker, and inner attempt timeout.</summary>
    public static IHttpClientBuilder AddEventHubResilience(
        this IHttpClientBuilder builder,
        string dependencyName)
    {
        builder.Services.AddOptions<EventHubResilienceOptions>()
            .BindConfiguration(EventHubResilienceOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => options.AttemptTimeoutSeconds < options.TotalTimeoutSeconds,
                "Resilience:AttemptTimeoutSeconds must be shorter than Resilience:TotalTimeoutSeconds.")
            .ValidateOnStart();

        builder.AddResilienceHandler(PipelineName, (pipeline, context) =>
        {
            context.EnableReloads<EventHubResilienceOptions>();
            var options = context.GetOptions<EventHubResilienceOptions>();
            var logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("EventHub.Resilience");

            // The total bound surrounds every retry. Each individual network attempt receives the smaller inner bound.
            pipeline.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(options.TotalTimeoutSeconds),
                OnTimeout = arguments =>
                {
                    LogTimeout(logger, dependencyName, "total", arguments.Timeout, arguments.Context);
                    return ValueTask.CompletedTask;
                }
            });

            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMilliseconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = arguments => ValueTask.FromResult(
                    IsRetryableRequest(arguments.Context.GetRequestMessage())
                    && IsTransient(arguments.Outcome, arguments.Context)),
                OnRetry = arguments =>
                {
                    LogRetry(
                        logger,
                        dependencyName,
                        arguments.AttemptNumber + 1,
                        arguments.RetryDelay,
                        arguments.Context);
                    return ValueTask.CompletedTask;
                }
            });

            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = options.CircuitFailureRatio,
                MinimumThroughput = options.CircuitMinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(options.CircuitSamplingSeconds),
                BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakSeconds),
                ShouldHandle = arguments => ValueTask.FromResult(IsTransient(arguments.Outcome, arguments.Context)),
                OnOpened = arguments =>
                {
                    LogCircuitTransition(
                        logger,
                        dependencyName,
                        "opened",
                        arguments.BreakDuration,
                        arguments.Context);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = arguments =>
                {
                    // Polly reports half-open using a retained context. Keep its originating trace
                    // so the log links back to the failure; closed links to the successful trial.
                    LogCircuitTransition(
                        logger,
                        dependencyName,
                        "half-open",
                        TimeSpan.Zero,
                        arguments.Context);
                    return ValueTask.CompletedTask;
                },
                OnClosed = arguments =>
                {
                    LogCircuitTransition(logger, dependencyName, "closed", TimeSpan.Zero, arguments.Context);
                    return ValueTask.CompletedTask;
                }
            });

            pipeline.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(options.AttemptTimeoutSeconds),
                OnTimeout = arguments =>
                {
                    LogTimeout(logger, dependencyName, "attempt", arguments.Timeout, arguments.Context);
                    return ValueTask.CompletedTask;
                }
            });
        });

        return builder;
    }

    /// <summary>Marks a write as safe to retry because its destination deduplicates the operation.</summary>
    public static void MarkAsIdempotent(this HttpRequestMessage request)
    {
        request.Options.Set(IdempotentRequestKey, true);
    }

    /// <summary>Allows GETs, explicitly marked writes, and requests carrying an application idempotency key.</summary>
    private static bool IsRetryableRequest(HttpRequestMessage? request)
    {
        if (request is null)
        {
            return false;
        }

        return request.Method == HttpMethod.Get
            || request.Method == HttpMethod.Post
                && (request.Headers.TryGetValues(IdempotencyHeaderName, out var keys)
                    && keys.Any(key => !string.IsNullOrWhiteSpace(key))
                    || request.Options.TryGetValue(IdempotentRequestKey, out var isIdempotent) && isIdempotent);
    }

    /// <summary>Captures the originating trace in Polly's per-call context before deferred callbacks can run.</summary>
    private static bool IsTransient(Outcome<HttpResponseMessage> outcome, ResilienceContext context)
    {
        if (!context.Properties.TryGetValue(TraceIdKey, out _))
        {
            context.Properties.Set(TraceIdKey, Activity.Current?.TraceId.ToString() ?? "none");
        }

        return HttpClientResiliencePredicates.IsTransient(outcome);
    }

    /// <summary>Writes a structured retry event that Aspire can correlate with the current distributed trace.</summary>
    private static void LogRetry(
        ILogger logger,
        string dependencyName,
        int attempt,
        TimeSpan delay,
        ResilienceContext context)
    {
        RetryLog(logger, dependencyName, attempt, delay.TotalMilliseconds, TraceId(context), null);
    }

    /// <summary>Records each breaker state change and its open duration for operational diagnosis.</summary>
    private static void LogCircuitTransition(
        ILogger logger,
        string dependencyName,
        string state,
        TimeSpan breakDuration,
        ResilienceContext context)
    {
        CircuitLog(
            logger,
            dependencyName,
            state,
            breakDuration.TotalSeconds,
            TraceId(context),
            null);
    }

    /// <summary>Distinguishes a single slow try from exhaustion of the overall call budget.</summary>
    private static void LogTimeout(
        ILogger logger,
        string dependencyName,
        string timeoutKind,
        TimeSpan timeout,
        ResilienceContext context)
    {
        TimeoutLog(logger, timeoutKind, dependencyName, timeout.TotalSeconds, TraceId(context), null);
    }

    /// <summary>Returns the trace captured for this call rather than a later callback's ambient activity.</summary>
    private static string TraceId(ResilienceContext context)
    {
        var ambientTraceId = Activity.Current?.TraceId.ToString();
        return context.Properties.GetValue(TraceIdKey, ambientTraceId ?? "none");
    }
}
