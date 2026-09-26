// Defines aggregate loading and insertion for Booking commands.
// Reads for screens use IBookingQueries instead, avoiding tracked entities and unnecessary Catalog calls.
namespace Booking.Application.Ports;

/// <summary>Tracked aggregate access used only when a booking is created or changed.</summary>
public interface IBookingRepository
{
    /// <summary>Loads the booking whose owner and cancellation rules must be checked.</summary>
    Task<Domain.Booking?> FindAsync(
        int id,
        CancellationToken cancellationToken);

    /// <summary>Tracks a confirmed booking for the next local save.</summary>
    void Add(Domain.Booking booking);
}
