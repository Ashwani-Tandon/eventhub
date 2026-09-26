namespace EventHub.BuildingBlocks.Results;

public sealed class Result<TValue> : IResult<Result<TValue>>
{
    private readonly TValue? _value;

    private Result(TValue? value, bool isSuccess, Error? error)
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

    public static Result<TValue> Success(TValue value) => new(value, true, null);

    public static Result<TValue> Failure(Error error) =>
        new(default, false, error ?? throw new ArgumentNullException(nameof(error)));
}
// Represents success with a value, or an expected failure with an Error.
// Reading Value on failure throws because the caller must check the outcome first.
