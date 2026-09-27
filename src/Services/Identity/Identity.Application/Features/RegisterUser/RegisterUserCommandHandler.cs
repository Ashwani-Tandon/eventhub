// Creates an attendee and issues a token after saving the hash.
// The mediator calls this use case; its ports keep framework details outside Application.
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using Identity.Domain;

namespace Identity.Application.Features.RegisterUser;

/// <summary>
/// Creates an attendee and issues a token after saving the hash. The mediator calls this use case; its ports keep framework details outside Application.
/// </summary>
public sealed class RegisterUserCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher hasher,
    IJwtTokenGenerator tokens,
    TimeProvider clock) : ICommandHandler<RegisterUserCommand, LoginResponse>
{
    /// <summary>
    /// Creates an Attendee with a password hash, saves it before issuing a JWT, and reports duplicate email conflicts.
    /// </summary>
    public async Task<Result<LoginResponse>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.FindByEmailAsync(email, cancellationToken) is not null)
        {
            return Result<LoginResponse>.Failure(UserErrors.DuplicateEmail);
        }

        var user = User.Create(Guid.NewGuid(), email, request.FullName, hasher.Hash(request.Password), clock.GetUtcNow());
        users.Add(user);
        // The unique database index also handles simultaneous registrations of the same email.
        if (!await unitOfWork.SaveAsync(cancellationToken))
        {
            return Result<LoginResponse>.Failure(UserErrors.DuplicateEmail);
        }

        // Give the user a login token only after their account is actually saved.
        return Result<LoginResponse>.Success(tokens.Generate(user));
    }
}
