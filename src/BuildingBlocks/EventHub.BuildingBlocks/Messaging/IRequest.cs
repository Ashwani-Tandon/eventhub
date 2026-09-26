using EventHub.BuildingBlocks.Results;

namespace EventHub.BuildingBlocks.Messaging;

public interface IRequest<TResponse>
    where TResponse : IResult<TResponse>;
