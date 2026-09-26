using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Debugging;

public static class DebugEndpoints
{
    public static IEndpointRouteBuilder MapDebugEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/debug/echo", HandleEchoAsync)
            .WithName("DebugEcho");

        return endpoints;
    }

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

    private sealed record EchoRequest(string Message);
}
