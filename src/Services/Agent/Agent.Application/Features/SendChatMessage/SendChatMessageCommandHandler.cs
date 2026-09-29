// Handles the chat use case after the mediator validates the submitted history.
// It checks the signed-in identity and delegates model mechanics to the Infrastructure port.
using Agent.Application.Contracts;
using Agent.Application.Ports;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
namespace Agent.Application.Features.SendChatMessage;

public sealed class SendChatMessageCommandHandler(IAgentChatClient client, ICurrentUser currentUser)
    : ICommandHandler<SendChatMessageCommand, ChatReply>
{
    /// <summary>Answers for the authenticated caller; validation has already checked that history exists.</summary>
    public Task<Result<ChatReply>> Handle(SendChatMessageCommand request, CancellationToken cancellationToken)
    {
        return currentUser.UserId is null
            ? Task.FromResult(Result<ChatReply>.Failure(AgentErrors.Unauthenticated))
            : client.ReplyAsync(request.Messages!, cancellationToken);
    }
}
