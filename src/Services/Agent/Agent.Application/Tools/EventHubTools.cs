// Implements event reads and confirmed booking actions that Ollama may request during a conversation.
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

    /// <summary>Reads an event by the ID supplied by the user or a prior search, instead of asking the model to make up its details.</summary>
    [Description("Get current facts for a real EventHub event using its numeric ID supplied by the user or SearchEvents.")]
    public async Task<string> GetEventDetails(
        [Description("Exact numeric event ID supplied by the user or returned by SearchEvents.")] int eventId,
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

    /// <summary>Creates one key per invocation; transport retries reuse it, while a new purchase gets a new key.</summary>
    [Description("Book tickets ONLY after stating the real event title, quantity and total INR and receiving a subsequent user confirmation. Never call to ask for confirmation. Returns the saved booking; never repeat this tool after success or an uncertain outcome.")]
    public async Task<string> BookTickets(
        [Description("Real event ID obtained from event tools and confirmed by the user.")] int eventId,
        [Description("User-confirmed ticket quantity, between 1 and 10.")] int quantity,
        CancellationToken cancellationToken = default)
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        LogTool(logger, nameof(BookTickets), JsonSerializer.Serialize(new { eventId, quantity, idempotencyKey }, Json), null);
        return Serialize(await booking.BookAsync(eventId, quantity, idempotencyKey, cancellationToken));
    }

    /// <summary>Uses the ID of a distinct owned booking; Booking checks ownership and returns its cancellation state.</summary>
    [Description("Cancel a booking ONLY after fetching GetMyBookings, stating its event title, quantity and total INR and receiving a subsequent user confirmation. If multiple bookings match, ask which booking ID first. Never invent IDs or cancel all matches.")]
    public async Task<string> CancelBooking(
        [Description("The specific booking ID from GetMyBookings that the user confirmed for cancellation.")] int bookingId,
        CancellationToken cancellationToken = default)
    {
        LogTool(logger, nameof(CancelBooking), JsonSerializer.Serialize(new { bookingId }, Json), null);
        return Serialize(await booking.CancelAsync(bookingId, cancellationToken));
    }

    /// <summary>Gets authorized sales facts; an attendee's request reaches Booking and receives its actual 403.</summary>
    [Description("Get sales statistics for the signed-in caller. Always call this tool for sales requests: Booking determines permission and scope. Forbidden means explain that the user does not have permission.")]
    public async Task<string> GetSalesStats(CancellationToken cancellationToken = default)
    {
        LogTool(logger, nameof(GetSalesStats), "{}", null);
        return Serialize(await booking.GetStatsAsync(cancellationToken));
    }

    /// <summary>Keeps tool failures readable while retaining the service's validation detail when filters are invalid.</summary>
    private static string Serialize<T>(Result<T> result)
    {
        if (result.IsSuccess) return JsonSerializer.Serialize(result.Value, Json);
        return result.Error!.Type switch
        {
            ErrorType.Forbidden => "Forbidden: you don't have permission",
            ErrorType.Unauthorized => "Please sign in again",
            ErrorType.NotFound => "Not found",
            _ => result.Error.Message
        };
    }
}
