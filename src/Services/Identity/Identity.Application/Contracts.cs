// Defines DTOs and narrow ports for persistence, hashing, and tokens.
// Handlers depend on these contracts so Infrastructure can supply adapters without leaking frameworks.
using Identity.Domain;
namespace Identity.Application;
public sealed record UserDto(Guid Id, string Email, string FullName, string Role);
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, UserDto User);
public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    void Add(User user);
}
public interface IUserQueries
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken);
}
public interface IUnitOfWork
{
    Task<bool> SaveAsync(CancellationToken cancellationToken);
}
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}
public interface IJwtTokenGenerator
{
    LoginResponse Generate(User user);
}
