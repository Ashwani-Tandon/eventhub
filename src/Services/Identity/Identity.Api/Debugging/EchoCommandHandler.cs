// Executes the demo command by echoing a message or triggering a requested failure.
// Its entry log proves whether validation allowed execution; this is a learning endpoint.
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;

namespace Identity.Api.Debugging;

/// <summary>
/// Executes the demo command by echoing a message or triggering a requested failure. Its entry log proves whether validation allowed execution; this is a learning endpoint.
/// </summary>
public sealed class EchoCommandHandler(
    ILogger<EchoCommandHandler> logger)
    : ICommandHandler<EchoCommand, EchoResponse>
{
    private static readonly Action<ILogger, int, Exception?> LogHandlerEntered =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(2000, nameof(LogHandlerEntered)),
            "Echo handler entered for a message with {MessageLength} characters");

    /// <summary>
    /// Returns the chosen development success, expected failure, or unexpected exception to exercise the full request pipeline.
    /// </summary>
    public Task<Result<EchoResponse>> Handle(
        EchoCommand request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        LogHandlerEntered(logger, request.Message.Length, null);

        if (request.ShouldThrow)
        {
            throw new InvalidOperationException("Debug echo exception.");
        }

        return Task.FromResult(
            request.FailureType is { } errorType
                ? Result<EchoResponse>.Failure(CreateError(errorType))
                : Result<EchoResponse>.Success(new EchoResponse(request.Message)));
    }

    /// <summary>
    /// Builds an example expected failure of the chosen category for the development HTTP demonstration.
    /// </summary>
    private static Error CreateError(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => Error.Validation(
                "Debug.Validation",
                "The debug handler returned a validation error.",
                new Dictionary<string, string[]>
                {
                    ["message"] = ["The debug handler rejected the message."]
                }),
            ErrorType.NotFound => Error.NotFound(
                "Debug.NotFound",
                "The requested debug resource was not found."),
            ErrorType.Conflict => Error.Conflict(
                "Debug.Conflict",
                "The debug request conflicts with the current state."),
            ErrorType.Forbidden => Error.Forbidden(
                "Debug.Forbidden",
                "The debug request is forbidden."),
            ErrorType.Unauthorized => Error.Unauthorized(
                "Debug.Unauthorized",
                "Authentication is required for this debug result."),
            ErrorType.Unavailable => Error.Unavailable(
                "Debug.Unavailable",
                "The debug dependency is unavailable."),
            ErrorType.Unprocessable => Error.Unprocessable(
                "Debug.Unprocessable",
                "The debug request could not be processed."),
            _ => throw new ArgumentOutOfRangeException(nameof(errorType), errorType, null)
        };
    }
}
