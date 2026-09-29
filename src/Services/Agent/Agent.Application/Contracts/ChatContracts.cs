// Defines the text history accepted by the assistant and its final reply.
// These plain records keep the chat use case independent of HTTP and the Ollama library.
namespace Agent.Application.Contracts;

/// <summary>A caller-supplied user or assistant message; callers cannot supply system or tool messages.</summary>
public sealed record ChatTurn(string Role, string Content);

/// <summary>The final answer that the future chat widget will display.</summary>
public sealed record ChatReply(string Reply);

/// <summary>Allowed history roles; the service owns its system instructions and tool results.</summary>
public static class HistoryRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";
}
