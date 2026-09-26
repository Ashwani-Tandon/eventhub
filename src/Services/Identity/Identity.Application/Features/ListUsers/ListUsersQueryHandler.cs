// Loads DTO projections through the dedicated read port.
// The mediator calls this use case; its ports keep framework details outside Application.
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
namespace Identity.Application.Features.ListUsers;
public sealed class ListUsersQueryHandler(IUserQueries users) : IQueryHandler<ListUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<Result<IReadOnlyList<UserDto>>> Handle(ListUsersQuery request, CancellationToken cancellationToken) =>
        Result<IReadOnlyList<UserDto>>.Success(await users.ListAsync(cancellationToken));
}
