using EventHub.BuildingBlocks.Results;

namespace EventHub.BuildingBlocks.Messaging;

public interface ICommand : IRequest<Result>;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
// Marks requests that change state, with or without a returned value.
// These contracts distinguish write intent from queries while always returning a Result.
