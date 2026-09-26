using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;

namespace Identity.Api.Debugging;

public sealed record EchoCommand(
    string Message,
    ErrorType? FailureType,
    bool ShouldThrow) : ICommand<EchoResponse>;

public sealed record EchoResponse(string Message);
// Defines the development demo request and its response.
// FailureType and ShouldThrow intentionally trigger error paths so we can observe the pipeline.
