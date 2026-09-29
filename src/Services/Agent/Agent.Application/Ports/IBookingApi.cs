// Exposes booking history and scoped sales without any write capability to the assistant.
// Infrastructure forwards the caller’s token; Booking owns validation, permissions and business rules.
using Agent.Application.Contracts;
using EventHub.BuildingBlocks.Results;
namespace Agent.Application.Ports;

public interface IBookingApi
{
    /// <summary>Returns saved booking snapshots even when Catalog is unavailable.</summary>
    Task<Result<IReadOnlyList<BookingFacts>>> GetMineAsync(CancellationToken cancellationToken);

    /// <summary>Lets Booking enforce organizer/admin permissions and choose the sales scope.</summary>
    Task<Result<SalesFacts>> GetStatsAsync(CancellationToken cancellationToken);
}
