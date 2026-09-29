// Implements the three read operations that Ollama may request during a conversation.
// Descriptions become the tool menu; each method calls a service port and returns compact JSON or a short error.
using System.ComponentModel;
using System.Text.Json;
using Agent.Application.Contracts;
using Agent.Application.Ports;
using EventHub.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;
namespace Agent.Application.Tools;

public sealed class EventHubTools(ICatalogApi catalog, IBookingApi booking, ILogger<EventHubTools> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly Action<ILogger, string, string, Exception?> LogTool = LoggerMessage.Define<string, string>(
        LogLevel.Information, new EventId(9001, nameof(LogTool)), "Agent tool {ToolName} arguments {Arguments}");

    /// <summary>Fetches filtered event facts, using Catalog's own filter validation and upcoming-date defaults.</summary>
    [Description("Search real EventHub events. Omit filters the user did not specify. Returns up to 20 matches plus total; empty items means no matches.")]
    public async Task<string> SearchEvents(
        [Description("Words in the event title; omit for a category-only search.")] string? search = null,
        [Description("One of Music, Tech, Sports, Comedy, or Workshop; omit if unspecified.")] string? category = null,
        [Description("City name; omit if unspecified.")] string? city = null,
        [Description("Maximum ticket price in INR; omit if unspecified.")] decimal? maxPrice = null,
        [Description("Optional start date/time, ISO 8601 with timezone.")] DateTimeOffset? from = null,
        [Description("Optional end date/time, ISO 8601 with timezone.")] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var filters = new EventFilters(search, category, city, maxPrice, from, to);
        LogTool(logger, nameof(SearchEvents), JsonSerializer.Serialize(filters, Json), null);
        var result = await catalog.SearchAsync(filters, cancellationToken);
        if (result.IsFailure) return Serialize(result);
        // Search answers need summary facts; long descriptions can be fetched separately with GetEventDetails.
        return JsonSerializer.Serialize(new
        {
            items = result.Value.Items.Select(x => new { x.Id, x.Title, x.Category, x.City, x.StartsAt, x.Price, x.SeatsLeft }),
            total = result.Value.Total
        }, Json);
    }

    /// <summary>Reads an event by the ID returned by a prior search, instead of asking the model to make up its details.</summary>
    [Description("Get current facts for a real EventHub event using its numeric ID from SearchEvents.")]
    public async Task<string> GetEventDetails(
        [Description("Numeric event ID returned by SearchEvents.")] int eventId,
        CancellationToken cancellationToken = default)
    {
        LogTool(logger, nameof(GetEventDetails), JsonSerializer.Serialize(new { eventId }, Json), null);
        return Serialize(await catalog.GetAsync(eventId, cancellationToken));
    }

    /// <summary>Uses the signed-in caller's JWT; no user identity is exposed as a tool argument.</summary>
    [Description("Read the signed-in user's newest 10 separate bookings with explicit recencyRank (1 is newest), ordered newest first, and total history count. Repeated titles are separate purchases with different IDs. Older records are omitted explicitly; do not claim the returned page is the full history.")]
    public async Task<string> GetMyBookings(CancellationToken cancellationToken = default)
    {
        LogTool(logger, nameof(GetMyBookings), "{}", null);
        var result = await booking.GetMineAsync(cancellationToken);
        if (result.IsFailure) return Serialize(result);
        // Booking returns newest first. Bound the model context, and disclose omitted history instead of silently truncating it.
        const int maximumBookings = 10;
        return JsonSerializer.Serialize(new
        {
            items = result.Value.Take(maximumBookings).Select((x, index) => new
            {
                recencyRank = index + 1, bookingId = x.Id, x.EventId, x.EventTitle,
                x.Quantity, x.Total, x.Status, x.SeatReleasePending
            }),
            ordering = "recencyRank 1 is newest; use ranks 1, 2, 3 for latest three. bookingId identifies each separate purchase. Do not merge repeated event titles.",
            total = result.Value.Count,
            olderBookingsOmitted = result.Value.Count > maximumBookings,
            scope = "Only a recent page. Older history is unknown here. List the requested records without claims about other bookings."
        }, Json);
    }

    /// <summary>Keeps tool failures readable while retaining the service's validation detail when filters are invalid.</summary>
    private static string Serialize<T>(Result<T> result)
    {
        if (result.IsSuccess) return JsonSerializer.Serialize(result.Value, Json);
        return result.Error!.Type switch
        {
            ErrorType.Forbidden => "You don't have permission",
            ErrorType.Unauthorized => "Please sign in again",
            ErrorType.NotFound => "Not found",
            _ => result.Error.Message
        };
    }
}
