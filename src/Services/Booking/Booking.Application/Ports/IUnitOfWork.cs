// Makes the local persistence boundary explicit to Booking handlers.
// A saved cancellation survives an unavailable Catalog so its seat release can be retried later.
using EventHub.BuildingBlocks.Results;

namespace Booking.Application.Ports;

/// <summary>Commits one local change before the handler proceeds to its next external operation.</summary>
public interface IUnitOfWork
{
    /// <summary>Saves tracked changes and returns an expected availability error for database faults.</summary>
    Task<Result> SaveAsync(CancellationToken cancellationToken);
}
