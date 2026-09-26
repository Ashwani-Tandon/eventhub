using EventHub.BuildingBlocks.Results;

namespace EventHub.BuildingBlocks.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
// Marks requests that read data and return a typed Result.
// Separate query contracts make read use cases easy to identify.
