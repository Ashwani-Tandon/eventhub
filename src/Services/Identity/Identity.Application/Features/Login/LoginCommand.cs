// Carries login credentials without logging their values.
// The API sends this request to the mediator, which selects its handler and validation pipeline.
using EventHub.BuildingBlocks.Messaging;
namespace Identity.Application.Features.Login;
public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResponse>;
