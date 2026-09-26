// Defines a request and the Result type it returns.
// Commands and queries share this contract so the mediator can dispatch both.
using EventHub.BuildingBlocks.Results;

namespace EventHub.BuildingBlocks.Messaging;

/// <summary>
/// Defines a request and the Result type it returns. Commands and queries share this contract so the mediator can dispatch both.
/// </summary>
public interface IRequest<TResponse>
    where TResponse : IResult<TResponse>;
