using EventHub.BuildingBlocks.Results;

namespace EventHub.BuildingBlocks.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
