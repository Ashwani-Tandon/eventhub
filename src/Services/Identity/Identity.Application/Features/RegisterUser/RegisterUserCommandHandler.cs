// Creates an attendee and issues a token after saving the hash.
// The mediator calls this use case; its ports keep framework details outside Application.
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using Identity.Domain;
namespace Identity.Application.Features.RegisterUser;
public sealed class RegisterUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork, IPasswordHasher hasher, IJwtTokenGenerator tokens, TimeProvider clock) : ICommandHandler<RegisterUserCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
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
        return Result<LoginResponse>.Success(tokens.Generate(user));
    }
}
