using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;

namespace Identity.Api.Debugging;

public sealed record EchoCommand(
    string Message,
    ErrorType? FailureType,
    bool ShouldThrow) : ICommand<EchoResponse>;

public sealed record EchoResponse(string Message);
