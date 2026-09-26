// Uses ASP.NET Core's salted password hash format.
// This adapter connects the application ports to the hosting infrastructure.
using Identity.Application;
using Microsoft.AspNetCore.Identity;
namespace Identity.Infrastructure;
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();
    private readonly object _subject = new();
    public string Hash(string password) => _hasher.HashPassword(_subject, password);
    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(_subject, hash, password) != PasswordVerificationResult.Failed;
}
