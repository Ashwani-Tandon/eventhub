// Exposes booking history, purchases, cancellation and scoped sales to the assistant.
// Infrastructure forwards the caller’s token; Booking owns validation, permissions and business rules.
using Agent.Application.Contracts;
using EventHub.BuildingBlocks.Results;
namespace Agent.Application.Ports;

public interface IBookingApi
{
    /// <summary>Returns saved booking snapshots even when Catalog is unavailable.</summary>
    Task<Result<IReadOnlyList<BookingFacts>>> GetMineAsync(CancellationToken cancellationToken);

    /// <summary>Creates a purchase with a stable identity for safe HTTP retries within this tool call.</summary>
    Task<Result<BookingFacts>> BookAsync(int eventId, int quantity, string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>Cancels a caller-owned purchase; this HTTP operation is not automatically retried.</summary>
    Task<Result<BookingFacts>> CancelAsync(int bookingId, CancellationToken cancellationToken);

    /// <summary>Lets Booking enforce organizer/admin permissions and choose the sales scope.</summary>
    Task<Result<SalesFacts>> GetStatsAsync(CancellationToken cancellationToken);
}
