// Represents success with a value, or an expected failure with an Error.
// Reading Value on failure throws because the caller must check the outcome first.
namespace EventHub.BuildingBlocks.Results;

/// <summary>
/// Represents success with a value, or an expected failure with an Error. Reading Value on failure throws because the caller must check the outcome first.
/// </summary>
public sealed class Result<TValue> : IResult<Result<TValue>>
{
    private readonly TValue? _value;

    /// <summary>
    /// Builds the internal success/error state; public factories control which outcomes callers can create.
    /// </summary>
    private Result(
        TValue? value,
        bool isSuccess,
        Error? error)
    {
        _value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public TValue Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    /// <summary>
    /// Creates a successful Result; the generic variant also carries the returned value.
    /// </summary>
    public static Result<TValue> Success(TValue value)
    {
        return new(value, true, null);
    }

    /// <summary>
    /// Creates a failed Result with its expected Error, which the HTTP boundary translates into ProblemDetails.
    /// </summary>
    public static Result<TValue> Failure(Error error)
    {
        return new(default, false, error ?? throw new ArgumentNullException(nameof(error)));
    }
}
