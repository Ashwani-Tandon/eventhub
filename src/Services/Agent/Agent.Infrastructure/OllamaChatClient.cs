// Runs one bounded assistant conversation with request-local tool instances.
// Microsoft.Extensions.AI executes the tool loop; a shared limiter protects the local model from too many chats at once.
using System.Globalization;
using System.Threading.RateLimiting;
using System.Text.Json;
using Agent.Application;
using Agent.Application.Contracts;
using Agent.Application.Ports;
using Agent.Application.Tools;
using EventHub.BuildingBlocks.Results;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Exceptions;
namespace Agent.Infrastructure;

public sealed class OllamaChatClient(IChatClient client, EventHubTools tools, ConcurrencyLimiter limiter,
    IOptions<OllamaOptions> options, TimeProvider clock, ILogger<OllamaChatClient> logger) : IAgentChatClient
{
    private static readonly Action<ILogger, Exception?> LogBusy = LoggerMessage.Define(
        LogLevel.Warning, new EventId(9029, nameof(LogBusy)), "Agent concurrency limit rejected chat: 2 active, queue capacity 5");
    private static readonly Action<ILogger, Exception?> LogOffline = LoggerMessage.Define(
        LogLevel.Warning, new EventId(9053, nameof(LogOffline)), "Assistant model connection or chat deadline failed; chat will not be retried");

    /// <summary>Builds server-owned instructions and tools, then returns only the final natural-language answer.</summary>
    public async Task<Result<ChatReply>> ReplyAsync(IReadOnlyList<ChatTurn> messages, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
        try
        {
            // A permit is one whole conversation, including tool calls. Two run, five wait; an eighth is refused immediately.
            using var lease = await limiter.AcquireAsync(1, deadline.Token);
            if (!lease.IsAcquired)
            {
                LogBusy(logger, null);
                return Result<ChatReply>.Failure(AgentErrors.Busy);
            }
            var history = new List<ChatMessage>
            {
                new(ChatRole.System, AssistantInstructions.SystemPrompt + "\nToday's date (UTC): " + clock.GetUtcNow().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            };
            foreach (var turn in messages)
            {
                history.Add(new(turn.Role == HistoryRoles.User ? ChatRole.User : ChatRole.Assistant, turn.Content));
            }
            var chatOptions = new ChatOptions
            {
                Temperature = 0.2f,
                // Bound answer length on the small local model; long lists should become concise summaries.
                MaxOutputTokens = 512,
                // Bind only these named methods, not every method discovered by reflection. Tools belong to this request's user.
                Tools = [AIFunctionFactory.Create(tools.SearchEvents), AIFunctionFactory.Create(tools.GetEventDetails), AIFunctionFactory.Create(tools.GetMyBookings),
                    AIFunctionFactory.Create(tools.PrepareBooking), AIFunctionFactory.Create(tools.PrepareCancellation),
                    AIFunctionFactory.Create(tools.GetSalesStats)]
            };
            // Six tool schemas plus confirmation history need more space than Ollama's default 4096-token window.
            // This is temporary conversation memory, not training; bounded history and tool pages still matter.
            chatOptions.AddOllamaOption(OllamaOption.NumCtx, 8192);
            // No retry wrapper: function invocation makes follow-up model turns, which are continuation, not retries.
            var response = await client.GetResponseAsync(history, chatOptions, deadline.Token);
            // The confirmation card and its lead-in come from service facts, even if model prose claims it already acted.
            if (tools.Proposal is { } proposal)
            {
                var prompt = proposal.Kind == ProposalKinds.Book
                    ? "Review this booking proposal. Nothing has been booked. Click Yes to book, or Cancel to dismiss."
                    : "Review this cancellation proposal. Nothing has been cancelled. Click Yes to cancel the booking, or Cancel to dismiss.";
                return Result<ChatReply>.Success(new ChatReply(prompt, proposal));
            }
            var reply = response.Messages.LastOrDefault(x => x.Role == ChatRole.Assistant)?.Text;
            return string.IsNullOrWhiteSpace(reply)
                ? Result<ChatReply>.Failure(AgentErrors.Offline)
                : Result<ChatReply>.Success(new ChatReply(reply));
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OllamaException
            || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // A local server outage and an expired deadline become friendly 503; a disconnected caller stays cancelled.
            LogOffline(logger, exception);
            return Result<ChatReply>.Failure(AgentErrors.Offline);
        }
    }
}
