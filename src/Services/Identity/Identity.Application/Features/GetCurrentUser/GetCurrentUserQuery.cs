// Requests the user's identity as captured in the authenticated token.
// The API sends this request to the mediator, which selects its handler and validation pipeline.
using EventHub.BuildingBlocks.Messaging;
namespace Identity.Application.Features.GetCurrentUser;
public sealed record GetCurrentUserQuery : IQuery<UserDto>;
