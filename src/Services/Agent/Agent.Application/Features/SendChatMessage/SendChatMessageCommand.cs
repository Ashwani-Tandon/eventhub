// Carries a complete text conversation through the same mediator used by the other services.
// No conversation is stored on the server; the caller sends earlier messages on every turn.
using Agent.Application.Contracts;
using EventHub.BuildingBlocks.Messaging;
namespace Agent.Application.Features.SendChatMessage;

public sealed record SendChatMessageCommand(IReadOnlyList<ChatTurn>? Messages) : ICommand<ChatReply>;
