// Names the Admin role used to widen organizer statistics to every organizer.
// It avoids duplicating string literals in Application without referencing another business service.
namespace Booking.Domain;

/// <summary>Role name used by Booking's local scope decision; token validation owns role authentication.</summary>
public static class BookingRoles
{
    public const string Admin = "Admin";
}
