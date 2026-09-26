// Marks requests that read data and return a typed Result.
// Separate query contracts make read use cases easy to identify.
using EventHub.BuildingBlocks.Results;

namespace EventHub.BuildingBlocks.Messaging;

/// <summary>
/// Marks requests that read data and return a typed Result. Separate query contracts make read use cases easy to identify.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
