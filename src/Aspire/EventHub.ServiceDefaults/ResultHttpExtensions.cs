// Translates handler Results into HTTP responses at the API boundary.
// Success becomes 200/204; expected failures become consistent ProblemDetails with matching status codes.
using EventHub.BuildingBlocks.Results;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Translates handler Results into HTTP responses at the API boundary. Success becomes 200/204; expected failures become consistent ProblemDetails with matching status codes.
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// Returns 201 with a resource location on success, or translates the expected error to ProblemDetails.
    /// </summary>
    public static IResult ToCreatedHttpResult<TValue>(
        this Result<TValue> result,
        string location)
    {
        return result.IsSuccess ? Results.Created(location, result.Value) : CreateProblem(result.Error!);
    }

    /// <summary>
    /// Maps the use-case result to its successful HTTP response or typed ProblemDetails failure.
    /// </summary>
    public static IResult ToHttpResult(this Result result)
    {
        return result.IsSuccess
            ? Results.NoContent()
            : CreateProblem(result.Error!);
    }

    /// <summary>
    /// Maps the use-case result to its successful HTTP response or typed ProblemDetails failure.
    /// </summary>
    public static IResult ToHttpResult<TValue>(this Result<TValue> result)
    {
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : CreateProblem(result.Error!);
    }

    /// <summary>
    /// Translates ErrorType into HTTP status and includes field messages for validation failures.
    /// </summary>
    private static IResult CreateProblem(Error error)
    {
// Turn problems such as "event missing" or "not enough seats" into the response number
        // the caller expects, such as 404 or 409. Every service uses this same translation.
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
            ErrorType.RateLimited => StatusCodes.Status429TooManyRequests,
            ErrorType.Unprocessable => StatusCodes.Status422UnprocessableEntity,
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, "Unknown error type.")
        };

        var extensions = new Dictionary<string, object?>
        {
            ["code"] = error.Code
        };

        if (error.Type == ErrorType.Validation)
        {
            return Results.ValidationProblem(
                error.ValidationErrors,
                detail: error.Message,
                statusCode: statusCode,
                title: "Validation failed",
                extensions: extensions);
        }

        return Results.Problem(
            detail: error.Message,
            statusCode: statusCode,
            title: error.Type.ToString(),
            extensions: extensions);
    }
}
