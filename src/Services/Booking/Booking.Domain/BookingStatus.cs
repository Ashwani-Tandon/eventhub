// Names the two customer-visible states of a booking.
// A separate pending flag records unfinished seat release without introducing another public status.
namespace Booking.Domain;

/// <summary>Shared status values used by the aggregate, SQL constraints, and response filters.</summary>
public static class BookingStatus
{
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";

    /// <summary>Accepts only the two states supported by the booking lifecycle.</summary>
    public static bool IsKnown(string status)
    {
        return status is Confirmed or Cancelled;
    }
}
