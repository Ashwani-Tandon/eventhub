// Exposes only the caller's booking history to the read-only assistant.
// Infrastructure calls Booking's own-history endpoint; the model cannot choose another user.
using Agent.Application.Contracts;
using EventHub.BuildingBlocks.Results;
namespace Agent.Application.Ports;

public interface IBookingApi
{
    /// <summary>Returns saved booking snapshots even when Catalog is unavailable.</summary>
    Task<Result<IReadOnlyList<BookingFacts>>> GetMineAsync(CancellationToken cancellationToken);
}
