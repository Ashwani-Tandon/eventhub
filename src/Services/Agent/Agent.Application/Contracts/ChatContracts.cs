// Defines the text history accepted by the assistant and its final reply.
// These plain records keep the chat use case independent of HTTP and the Ollama library.
namespace Agent.Application.Contracts;

/// <summary>A caller-supplied user or assistant message; callers cannot supply system or tool messages.</summary>
public sealed record ChatTurn(string Role, string Content);

/// <summary>Text plus an optional service-derived card; model prose is never parsed into a purchase.</summary>
public sealed record ChatReply(string Reply, ActionProposal? Action = null);

/// <summary>One read-only suggestion. The browser executes existing Booking APIs only after its Yes button.</summary>
public sealed record ActionProposal(string Kind, int EventId, int? BookingId, string EventTitle,
    DateTimeOffset EventStartsAt, int Quantity, decimal UnitPrice, decimal Total);

/// <summary>Shared proposal names keep the server and browser action contract explicit.</summary>
public static class ProposalKinds
{
    public const string Book = "book";
    public const string Cancel = "cancel";
}

/// <summary>Allowed history roles; the service owns its system instructions and tool results.</summary>
public static class HistoryRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";
}
