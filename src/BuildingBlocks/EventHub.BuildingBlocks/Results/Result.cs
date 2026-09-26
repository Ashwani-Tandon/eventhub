// Represents success or an expected failure for an operation with no returned data.
// Private construction ensures a failure carries an Error and success carries none.
namespace EventHub.BuildingBlocks.Results;

/// <summary>
/// Represents success or an expected failure for an operation with no returned data. Private construction ensures a failure carries an Error and success carries none.
/// </summary>
public sealed class Result : IResult<Result>
{
    /// <summary>
    /// Builds the internal success/error state; public factories control which outcomes callers can create.
    /// </summary>
    private Result(
        bool isSuccess,
        Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    /// <summary>
    /// Creates a successful Result; the generic variant also carries the returned value.
    /// </summary>
    public static Result Success()
    {
        return new(true, null);
    }

    /// <summary>
    /// Creates a failed Result with its expected Error, which the HTTP boundary translates into ProblemDetails.
    /// </summary>
    public static Result Failure(Error error)
    {
        return new(false, error ?? throw new ArgumentNullException(nameof(error)));
    }
}
