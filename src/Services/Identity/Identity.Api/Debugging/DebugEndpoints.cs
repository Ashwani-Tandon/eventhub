// Maps POST /debug/echo and translates HTTP input into an EchoCommand.
// It sends the command through ISender and maps its Result back to HTTP; Program enables it only in Development.
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Debugging;

/// <summary>
/// Maps POST /debug/echo and translates HTTP input into an EchoCommand. It sends the command through ISender and maps its Result back to HTTP; Program enables it only in Development.
/// </summary>
public static class DebugEndpoints
{
    /// <summary>
    /// Registers the development echo endpoint used to observe mediator behaviors and error mapping.
    /// </summary>
    public static IEndpointRouteBuilder MapDebugEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/debug/echo", HandleEchoAsync)
            .WithName("DebugEcho");

        return endpoints;
    }

    /// <summary>
    /// Reads the development echo body, sends its command through the mediator, and maps the returned Result to HTTP.
    /// </summary>
    private static async Task<Microsoft.AspNetCore.Http.IResult> HandleEchoAsync(
        [FromBody] EchoRequest request,
        [FromQuery] string? fail,
        [FromQuery(Name = "throw")] bool? shouldThrow,
        ISender sender,
        CancellationToken cancellationToken)
    {
        ErrorType? failureType = null;

        if (!string.IsNullOrWhiteSpace(fail))
        {
            if (!Enum.TryParse<ErrorType>(fail, ignoreCase: true, out var parsedErrorType))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["fail"] = [$"'{fail}' is not a recognized error type."]
                    });
            }

            failureType = parsedErrorType;
        }

        var result = await sender.Send(
            new EchoCommand(request.Message, failureType, shouldThrow ?? false),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// Maps POST /debug/echo and translates HTTP input into an EchoCommand. It sends the command through ISender and maps its Result back to HTTP; Program enables it only in Development.
    /// </summary>
    private sealed record EchoRequest(string Message);
}
