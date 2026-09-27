// Uses ASP.NET Core's salted password hash format.
// This adapter connects the application ports to the hosting infrastructure.
using Identity.Application;
using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure;

/// <summary>
/// Uses ASP.NET Core's salted password hash format. This adapter connects the application ports to the hosting infrastructure.
/// </summary>
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    private readonly object _subject = new();

    /// <summary>
    /// Creates a salted password hash using ASP.NET Core's password hasher; the plaintext is never stored.
    /// </summary>
    public string Hash(string password)
    {
        return _hasher.HashPassword(_subject, password);
    }

    /// <summary>
    /// Checks the submitted password against its stored hash, accepting successful checks that need future rehashing.
    /// </summary>
    public bool Verify(
        string hash,
        string password)
    {
        // The library may say "correct password, but stored protection could be upgraded".
        // Allow login in that case; this version does not yet upgrade the stored password protection.
        return _hasher.VerifyHashedPassword(_subject, hash, password) != PasswordVerificationResult.Failed;
    }
}
