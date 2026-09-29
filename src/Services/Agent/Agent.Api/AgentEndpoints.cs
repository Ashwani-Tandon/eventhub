// Exposes the authenticated chat use case through one thin HTTP endpoint.
// History comes from the JSON body, the mediator from DI, and cancellation from the request itself.
using Agent.Application.Contracts;
using Agent.Application.Features.SendChatMessage;
using EventHub.BuildingBlocks.Messaging;
using Microsoft.AspNetCore.Mvc;
namespace Agent.Api;

public static class AgentEndpoints
{
    /// <summary>Accepts caller history; validation, model work and error decisions stay in the mediator pipeline.</summary>
    public static void MapAgentEndpoints(this WebApplication app)
    {
        app.MapPost("/chat", async ([FromBody] ChatInput input, [FromServices] ISender sender, CancellationToken ct) =>
            (await sender.Send(new SendChatMessageCommand(input.Messages), ct)).ToHttpResult())
            .RequireAuthorization();
        // CancellationToken is ASP.NET Core's request-cancellation context, not JSON input or a DI service.
    }
}

/// <summary>Only user/assistant text is accepted; system instructions and tool results are created by the server.</summary>
public sealed record ChatInput(IReadOnlyList<ChatTurn>? Messages);
