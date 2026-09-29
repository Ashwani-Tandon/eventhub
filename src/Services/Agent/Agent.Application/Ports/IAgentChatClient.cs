// Defines the chat operation the Application handler needs from a language-model adapter.
// Infrastructure owns model connections, tool invocation, waiting limits, and failure translation.
using Agent.Application.Contracts;
using EventHub.BuildingBlocks.Results;
namespace Agent.Application.Ports;

public interface IAgentChatClient
{
    /// <summary>Answers using the supplied history and the service's read-only tools, without storing conversation state.</summary>
    Task<Result<ChatReply>> ReplyAsync(IReadOnlyList<ChatTurn> messages, CancellationToken cancellationToken);
}
