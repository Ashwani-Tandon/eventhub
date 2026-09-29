// Implements the assistant’s booking reads through Booking’s API.
// The forwarded JWT governs access; chat has no purchase or cancellation HTTP methods.
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

    /// <summary>Calls the protected endpoint even for attendees, leaving the permission decision with Booking.</summary>
    public Task<Result<SalesFacts>> GetStatsAsync(CancellationToken cancellationToken)
        => ServiceReader.ReadAsync<SalesFacts>(client, "/bookings/stats", "Booking", logger, cancellationToken);
}
