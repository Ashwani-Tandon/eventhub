// Loads DTO projections through the dedicated read port.
// The mediator calls this use case; its ports keep framework details outside Application.
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;

namespace Identity.Application.Features.ListUsers;

/// <summary>
/// Loads DTO projections through the dedicated read port. The mediator calls this use case; its ports keep framework details outside Application.
/// </summary>
public sealed class ListUsersQueryHandler(IUserQueries users) : IQueryHandler<ListUsersQuery, IReadOnlyList<UserDto>>
{
    /// <summary>
    /// Reads safe user DTOs; the Admin endpoint policy authorizes this operation before it reaches the handler.
    /// </summary>
    public async Task<Result<IReadOnlyList<UserDto>>> Handle(
        ListUsersQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyList<UserDto>>.Success(await users.ListAsync(cancellationToken));
    }
}
