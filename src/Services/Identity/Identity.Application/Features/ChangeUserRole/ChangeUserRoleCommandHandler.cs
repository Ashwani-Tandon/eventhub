// Persists a domain role transition for future logins.
// The mediator calls this use case; its ports keep framework details outside Application.
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using Identity.Domain;

namespace Identity.Application.Features.ChangeUserRole;

/// <summary>
/// Persists a domain role transition for future logins. The mediator calls this use case; its ports keep framework details outside Application.
/// </summary>
public sealed class ChangeUserRoleCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork) : ICommandHandler<ChangeUserRoleCommand, UserDto>
{
    /// <summary>
    /// Loads the selected user, validates the role through Domain, saves it, and returns the changed profile.
    /// </summary>
    public async Task<Result<UserDto>> Handle(
        ChangeUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(request.Id, cancellationToken);
        if (user is null)
        {
            return Result<UserDto>.Failure(UserErrors.NotFound);
        }

        var result = user.ChangeRole(request.Role);
        if (result.IsFailure)
        {
            return Result<UserDto>.Failure(result.Error!);
        }
        await unitOfWork.SaveAsync(cancellationToken);
        return Result<UserDto>.Success(new(user.Id, user.Email, user.FullName, user.Role));
    }
}
