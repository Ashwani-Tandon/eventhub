// Requests read-only user projections for the admin screen.
// The API sends this request to the mediator, which selects its handler and validation pipeline.
using EventHub.BuildingBlocks.Messaging;
namespace Identity.Application.Features.ListUsers;
public sealed record ListUsersQuery : IQuery<IReadOnlyList<UserDto>>;
