// Registers identical token validation and policies in every service.
// APIs use the same signing settings but validate each request independently of the gateway.
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using EventHub.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
namespace Microsoft.Extensions.Hosting;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    [Required, MinLength(32)] public string Key { get; set; } = "";
    [Required] public string Issuer { get; set; } = "eventhub-identity";
    [Required] public string Audience { get; set; } = "eventhub";
    [Range(120, 120)] public int LifetimeMinutes { get; set; } = 120;
}
public static class AuthPolicies
{
    public const string Organizer = "Organizer";
    public const string Admin = "Admin";
}
public static class TokenClaims
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
}
public static class AuthenticationExtensions
{
    public static TBuilder AddEventHubAuth<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>>((options, jwt) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Value.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Key)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = TokenClaims.Subject,
                    RoleClaimType = TokenClaims.Role
                };
            });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.Organizer, policy => policy.RequireRole(AuthPolicies.Organizer, AuthPolicies.Admin))
            .AddPolicy(AuthPolicies.Admin, policy => policy.RequireRole(AuthPolicies.Admin));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserProfile, HttpCurrentUser>();
        builder.Services.AddScoped<ICurrentUser>(services => services.GetRequiredService<ICurrentUserProfile>());
        return builder;
    }
}
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUserProfile
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User.Identity?.IsAuthenticated == true ? accessor.HttpContext.User : null;
    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(TokenClaims.Subject), out var id) ? id : null;
    public string? Role => Principal?.FindFirstValue(TokenClaims.Role);
    public string? Email => Principal?.FindFirstValue(TokenClaims.Email);
    public string? FullName => Principal?.FindFirstValue(TokenClaims.Name);
}
