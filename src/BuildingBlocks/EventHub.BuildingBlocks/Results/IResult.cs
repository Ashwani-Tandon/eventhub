// Exposes the common success/error shape for both Result variants.
// The static Failure contract lets validation create the correct response type without reflection.
namespace EventHub.BuildingBlocks.Results;

/// <summary>
/// Exposes the common success/error shape for both Result variants. The static Failure contract lets validation create the correct response type without reflection.
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }

    bool IsFailure { get; }

    Error? Error { get; }
}

/// <summary>
/// Exposes the common success/error shape for both Result variants. The static Failure contract lets validation create the correct response type without reflection.
/// </summary>
public interface IResult<TSelf> : IResult
    where TSelf : IResult<TSelf>
{
    static abstract TSelf Failure(Error error);
}
