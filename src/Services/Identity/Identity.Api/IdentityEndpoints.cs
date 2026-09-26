// Translates HTTP requests into commands and queries.
// Policies protect admin routes; the mediator handles validation and business decisions.
using EventHub.BuildingBlocks.Messaging;
using Identity.Application.Features.ChangeUserRole;
using Identity.Application.Features.GetCurrentUser;
using Identity.Application.Features.ListUsers;
using Identity.Application.Features.Login;
using Identity.Application.Features.RegisterUser;

namespace Identity.Api;

/// <summary>
/// Translates HTTP requests into commands and queries. Policies protect admin routes; the mediator handles validation and business decisions.
/// </summary>
public static class IdentityEndpoints
{
    /// <summary>
    /// Registers the five Identity operations and applies Admin policies to user listing and role changes.
    /// </summary>
    public static void MapIdentityEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/register", async (RegisterUserCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToCreatedHttpResult("/identity/auth/me")).AllowAnonymous();
        app.MapPost("/auth/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttpResult()).AllowAnonymous();
        app.MapGet("/auth/me", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetCurrentUserQuery(), ct)).ToHttpResult()).RequireAuthorization();
        app.MapGet("/users", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListUsersQuery(), ct)).ToHttpResult()).RequireAuthorization(AuthPolicies.Admin);
        app.MapPut("/users/{id:guid}/role", async (Guid id, ChangeRoleInput input, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ChangeUserRoleCommand(id, input.Role), ct)).ToHttpResult()).RequireAuthorization(AuthPolicies.Admin);
    }
}

/// <summary>
/// Translates HTTP requests into commands and queries. Policies protect admin routes; the mediator handles validation and business decisions.
/// </summary>
public sealed record ChangeRoleInput(string Role);
