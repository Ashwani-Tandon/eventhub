// Carries an administrator's requested role change.
// The API sends this request to the mediator, which selects its handler and validation pipeline.
using EventHub.BuildingBlocks.Messaging;

namespace Identity.Application.Features.ChangeUserRole;

/// <summary>
/// Carries an administrator's requested role change. The API sends this request to the mediator, which selects its handler and validation pipeline.
/// </summary>
public sealed record ChangeUserRoleCommand(
    Guid Id,
    string Role) : ICommand<UserDto>;
