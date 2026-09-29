// Implements only the assistant's own-booking read through Booking's API.
// The forwarded JWT selects the caller's records, and stored event snapshots avoid any Catalog dependency here.
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
}
