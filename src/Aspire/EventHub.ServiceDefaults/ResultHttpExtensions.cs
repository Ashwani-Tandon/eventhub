using EventHub.BuildingBlocks.Results;

namespace Microsoft.AspNetCore.Http;

public static class ResultHttpExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess
            ? Results.NoContent()
            : CreateProblem(result.Error!);

    public static IResult ToHttpResult<TValue>(this Result<TValue> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : CreateProblem(result.Error!);

    private static IResult CreateProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
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
