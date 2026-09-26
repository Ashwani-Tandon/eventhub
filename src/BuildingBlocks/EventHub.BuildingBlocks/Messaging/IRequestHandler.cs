// Defines the handler that executes one command or query.
// The mediator resolves its matching handler from DI; business use cases implement these contracts.
using EventHub.BuildingBlocks.Results;

namespace EventHub.BuildingBlocks.Messaging;

/// <summary>
/// Defines the handler that executes one command or query. The mediator resolves its matching handler from DI; business use cases implement these contracts.
/// </summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Defines the handler that executes one command or query. The mediator resolves its matching handler from DI; business use cases implement these contracts.
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

/// <summary>
/// Defines the handler that executes one command or query. The mediator resolves its matching handler from DI; business use cases implement these contracts.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

/// <summary>
/// Defines the handler that executes one command or query. The mediator resolves its matching handler from DI; business use cases implement these contracts.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
    : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
