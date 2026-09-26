// Reads token claims so existing tokens retain their original role.
// The mediator calls this use case; its ports keep framework details outside Application.
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using Identity.Domain;
namespace Identity.Application.Features.GetCurrentUser;
public sealed class GetCurrentUserQueryHandler(ICurrentUserProfile currentUser) : IQueryHandler<GetCurrentUserQuery, UserDto>
{
    public Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(currentUser.UserId is Guid id
            ? Result<UserDto>.Success(new(id, currentUser.Email!, currentUser.FullName!, currentUser.Role!))
            : Result<UserDto>.Failure(UserErrors.Unauthenticated));
    }
}
