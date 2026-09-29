// Implements event reads and read-only booking proposals that Ollama may request during a conversation.
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
    // This instance belongs to one request. Keep at most one proposal so several tool calls cannot swap the card.
    public ActionProposal? Proposal { get; private set; }

    private const string CancelledStatus = "Cancelled";
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

    /// <summary>Fetches the real event and calculates a preview; it never reserves, pays or creates a booking.</summary>
    [Description("Prepare a booking confirmation card, without booking anything. Call when the user asks to book tickets. Use the exact event ID and requested quantity; the UI will ask Yes / Cancel and handle execution.")]
    public async Task<string> PrepareBooking(
        [Description("Exact event ID supplied by the user or found through SearchEvents.")] int eventId,
        [Description("Requested ticket quantity between 1 and 10; ask the user if quantity is missing.")] int quantity,
        CancellationToken cancellationToken = default)
    {
        LogTool(logger, nameof(PrepareBooking), JsonSerializer.Serialize(new { eventId, quantity }, Json), null);
        if (Proposal is not null) return "One confirmation card is already prepared. Stop and let the user review it.";
        if (quantity is < 1 or > 10) return "Quantity must be between 1 and 10";
        var result = await catalog.GetAsync(eventId, cancellationToken);
        if (result.IsFailure) return Serialize(result);
        var item = result.Value;
        Proposal = new(ProposalKinds.Book, item.Id, null, item.Title, item.StartsAt,
            quantity, item.Price, item.Price * quantity);
        return JsonSerializer.Serialize(new { proposal = Proposal, instruction = "Nothing booked. Review the UI card and click Yes to submit." }, Json);
    }

    /// <summary>Looks up the full caller-owned history, rejecting invented/other-user IDs before making a cancellation card.</summary>
    [Description("Prepare a cancellation confirmation card without cancelling anything. Use a booking ID from GetMyBookings or explicitly supplied by the user. If multiple purchases match an event name, ask which ID. The UI will handle Yes / Cancel.")]
    public async Task<string> PrepareCancellation(
        [Description("The specific owned booking ID selected by the user, not an event ID.")] int bookingId,
        CancellationToken cancellationToken = default)
    {
        LogTool(logger, nameof(PrepareCancellation), JsonSerializer.Serialize(new { bookingId }, Json), null);
        if (Proposal is not null) return "One confirmation card is already prepared. Stop and let the user review it.";
        var result = await booking.GetMineAsync(cancellationToken);
        if (result.IsFailure) return Serialize(result);
        // The API scopes this list to the JWT's owner. Search all returned records, not only the model's recent page.
        var item = result.Value.FirstOrDefault(x => x.Id == bookingId);
        if (item is null) return "Not found in your bookings. Use a booking ID from My Bookings.";
        if (item.Status == CancelledStatus && !item.SeatReleasePending) return "This booking is already cancelled";
        Proposal = new(ProposalKinds.Cancel, item.EventId, item.Id, item.EventTitle, item.EventStartsAt,
            item.Quantity, item.Total / item.Quantity, item.Total);
        return JsonSerializer.Serialize(new { proposal = Proposal, instruction = "Nothing cancelled. Review the UI card and click Yes to submit." }, Json);
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
