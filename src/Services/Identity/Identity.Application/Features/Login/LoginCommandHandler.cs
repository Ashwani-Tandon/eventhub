// Authenticates a password and returns the same failure for unknown users.
// The mediator calls this use case; its ports keep framework details outside Application.
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using Identity.Domain;

namespace Identity.Application.Features.Login;

/// <summary>
/// Authenticates a password and returns the same failure for unknown users. The mediator calls this use case; its ports keep framework details outside Application.
/// </summary>
public sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenGenerator tokens) : ICommandHandler<LoginCommand, LoginResponse>
{
    /// <summary>
    /// Authenticates the normalized email and password; unknown users and wrong passwords share the same failure to avoid revealing registered emails.
    /// </summary>
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !hasher.Verify(user.PasswordHash, request.Password))
        {
            return Result<LoginResponse>.Failure(UserErrors.InvalidCredentials);
        }

        return Result<LoginResponse>.Success(tokens.Generate(user));
    }
}
