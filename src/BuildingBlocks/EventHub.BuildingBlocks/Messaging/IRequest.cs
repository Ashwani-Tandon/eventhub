using EventHub.BuildingBlocks.Results;

namespace EventHub.BuildingBlocks.Messaging;

public interface IRequest<TResponse>
    where TResponse : IResult<TResponse>;
// Defines a request and the Result type it returns.
// Commands and queries share this contract so the mediator can dispatch both.
