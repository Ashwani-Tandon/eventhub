// Implements the assistant’s booking reads and actions through Booking’s API.
// The forwarded JWT governs access; a per-purchase key keeps HTTP retries from duplicating a booking.
using System.Net.Http.Json;
using System.Globalization;
using Agent.Application.Contracts;
using Agent.Application.Ports;
using EventHub.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;
namespace Agent.Infrastructure;

public sealed class BookingApi(HttpClient client, ILogger<BookingApi> logger) : IBookingApi
{
    /// <summary>Reads the caller's booking snapshots without accepting a user ID from chat text.</summary>
    public Task<Result<IReadOnlyList<BookingFacts>>> GetMineAsync(CancellationToken cancellationToken)
        => ServiceReader.ReadAsync<IReadOnlyList<BookingFacts>>(client, "/bookings/mine", "Booking", logger, cancellationToken);

    /// <summary>Sends the key on the request before the shared pipeline starts, so every attempt has the same identity.</summary>
    public async Task<Result<BookingFacts>> BookAsync(int eventId, int quantity, string idempotencyKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/bookings")
        {
            Content = JsonContent.Create(new { eventId, quantity })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await ServiceReader.SendAsync<BookingFacts>(client, request, "Booking", logger, cancellationToken);
    }

    /// <summary>Uses a POST without an idempotency header; shared resilience therefore disables automatic retries.</summary>
    public async Task<Result<BookingFacts>> CancelAsync(int bookingId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,
            "/bookings/" + bookingId.ToString(CultureInfo.InvariantCulture) + "/cancel");
        return await ServiceReader.SendAsync<BookingFacts>(client, request, "Booking", logger, cancellationToken);
    }

    /// <summary>Calls the protected endpoint even for attendees, leaving the permission decision with Booking.</summary>
    public Task<Result<SalesFacts>> GetStatsAsync(CancellationToken cancellationToken)
        => ServiceReader.ReadAsync<SalesFacts>(client, "/bookings/stats", "Booking", logger, cancellationToken);
}
