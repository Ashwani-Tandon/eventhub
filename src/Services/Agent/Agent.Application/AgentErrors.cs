// Names expected assistant failures independently of HTTP response numbers.
// The API uses the shared Result mapper to turn these into friendly 401, 429, or 503 responses.
using EventHub.BuildingBlocks.Results;
namespace Agent.Application;

public static class AgentErrors
{
    public static readonly Error Unauthenticated = Error.Unauthorized("Agent.Unauthenticated", "Please sign in to use the assistant.");
    public static readonly Error Offline = Error.Unavailable("Agent.Offline", "The assistant is offline");
    public static readonly Error Busy = new("Agent.Busy", "The assistant is busy, try again shortly", ErrorType.RateLimited);
}
