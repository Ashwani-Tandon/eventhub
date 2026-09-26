// Carries public registration input through the mediator.
// The API sends this request to the mediator, which selects its handler and validation pipeline.
using EventHub.BuildingBlocks.Messaging;
namespace Identity.Application.Features.RegisterUser;
public sealed record RegisterUserCommand(string Email, string FullName, string Password) : ICommand<LoginResponse>;
