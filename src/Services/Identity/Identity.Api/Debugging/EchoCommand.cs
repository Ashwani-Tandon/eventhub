// Defines the development demo request and its response.
// FailureType and ShouldThrow intentionally trigger error paths so we can observe the pipeline.
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;

namespace Identity.Api.Debugging;

/// <summary>
/// Defines the development demo request and its response. FailureType and ShouldThrow intentionally trigger error paths so we can observe the pipeline.
/// </summary>
public sealed record EchoCommand(
    string Message,
    ErrorType? FailureType,
    bool ShouldThrow) : ICommand<EchoResponse>;

/// <summary>
/// Defines the development demo request and its response. FailureType and ShouldThrow intentionally trigger error paths so we can observe the pipeline.
/// </summary>
public sealed record EchoResponse(string Message);
