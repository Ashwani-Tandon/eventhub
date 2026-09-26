namespace EventHub.BuildingBlocks.Results;

public interface IResult
{
    bool IsSuccess { get; }

    bool IsFailure { get; }

    Error? Error { get; }
}

public interface IResult<TSelf> : IResult
    where TSelf : IResult<TSelf>
{
    static abstract TSelf Failure(Error error);
}
