// Signs a two-hour access token with explicit profile claims.
// This adapter connects the application ports to the hosting infrastructure.
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application;
using Identity.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure;

/// <summary>
/// Signs a two-hour access token with explicit profile claims. This adapter connects the application ports to the hosting infrastructure.
/// </summary>
public sealed class JwtTokenGenerator(
    IOptions<JwtOptions> options,
    TimeProvider clock) : IJwtTokenGenerator
{
    /// <summary>
    /// Creates a signed token containing the user's current profile and an explicit expiry.
    /// </summary>
    public LoginResponse Generate(User user)
    {
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(options.Value.LifetimeMinutes);
        // The login token remembers user details at login time. After Admin changes their role,
        // they must log in again to get a token showing the new role.
        // Add a digital signature so services can spot changed tokens; the details are still readable.
        var token = new JwtSecurityToken(options.Value.Issuer, options.Value.Audience,
            [new Claim(TokenClaims.Subject, user.Id.ToString()), new Claim(TokenClaims.Email, user.Email),
             new Claim(TokenClaims.Name, user.FullName), new Claim(TokenClaims.Role, user.Role)],
            now.UtcDateTime, expires.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key)), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires, new(user.Id, user.Email, user.FullName, user.Role));
    }
}
