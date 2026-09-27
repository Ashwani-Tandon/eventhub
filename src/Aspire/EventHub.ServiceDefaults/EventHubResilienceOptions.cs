// Holds the shared timeout, retry, and circuit-breaker settings used by service HTTP clients.
// Configuration binding keeps operational tuning outside code while validation rejects unsafe values at startup.
using System.ComponentModel.DataAnnotations;

namespace EventHub.ServiceDefaults;

/// <summary>Configurable values for the shared service-to-service resilience pipeline.</summary>
public sealed class EventHubResilienceOptions
{
    public const string SectionName = "Resilience";

    [Range(1, 300)]
    public int TotalTimeoutSeconds { get; init; } = 10;

    [Range(0, 10)]
    public int MaxRetryAttempts { get; init; } = 3;

    [Range(1, 60_000)]
    public int RetryBaseDelayMilliseconds { get; init; } = 200;

    [Range(0.01, 1)]
    public double CircuitFailureRatio { get; init; } = 0.5;

    [Range(2, 10_000)]
    public int CircuitMinimumThroughput { get; init; } = 5;

    [Range(1, 300)]
    public int CircuitSamplingSeconds { get; init; } = 10;

    [Range(1, 300)]
    public int CircuitBreakSeconds { get; init; } = 15;

    [Range(1, 300)]
    public int AttemptTimeoutSeconds { get; init; } = 2;
}
