// Defines safe Identity response models and the ports used by Identity handlers.
// Infrastructure implements these ports so Application does not depend on SQL, hashing, or JWT libraries.
using Identity.Domain;

namespace Identity.Application;

/// <summary>
/// Profile returned to callers; password hashes are never included in this response.
/// </summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role);

/// <summary>
/// Successful login or registration response containing a signed token, expiry, and its user profile.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserDto User);

/// <summary>
/// Loads users for commands and tracks new registrations until the unit of work saves.
/// </summary>
public interface IUserRepository
{
    /// <summary>Finds the normalized email for login or duplicate-registration checks.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Loads the user targeted by an administrator's role-change command.</summary>
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Tracks a newly registered user for insertion when the unit of work saves.</summary>
    void Add(User user);
}

/// <summary>
/// Read-only user listing for Admin; persistence projects directly to safe profile fields.
/// </summary>
public interface IUserQueries
{
    /// <summary>Returns profiles without tracking entities or exposing password hashes.</summary>
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Commits Identity changes and reports a competing duplicate-email registration to the handler.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Returns false when another registration saved the same email first.</summary>
    Task<bool> SaveAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Allows registration and login to share a password hash format without knowing the hashing library.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Creates a salted hash for storage instead of storing the plaintext password.</summary>
    string Hash(string password);

    /// <summary>Checks the submitted password against its stored salted hash.</summary>
    bool Verify(string hash, string password);
}

/// <summary>
/// Issues a signed snapshot of the user after successful registration or login.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>Returns the token, expiry, and profile with the user's current role.</summary>
    LoginResponse Generate(User user);
}
