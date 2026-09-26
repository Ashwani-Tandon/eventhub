// Generates the one deterministic event-and-booking dataset shared by Catalog and Booking seeds.
// Fixed identifiers and random seed keep cross-service references and demo totals aligned.
namespace EventHub.SeedData;

/// <summary>
/// Development seed fields shared between the service seeds so event references and booked-seat totals agree.
/// </summary>
public sealed record SeedEvent(
    int Id,
    string Title,
    string Description,
    string Category,
    string Venue,
    string City,
    DateTimeOffset StartsAt,
    decimal Price,
    int Capacity,
    Guid OrganizerId,
    DateTimeOffset CreatedAt);

/// <summary>
/// Development seed fields shared between the service seeds so event references and booked-seat totals agree.
/// </summary>
public sealed record SeedBooking(
    Guid Id,
    int EventId,
    Guid AttendeeId,
    int Quantity,
    string Status,
    DateTimeOffset BookedAt);

/// <summary>
/// Development seed fields shared between the service seeds so event references and booked-seat totals agree.
/// </summary>
public sealed record DemoDataSet(
    IReadOnlyList<SeedEvent> Events,
    IReadOnlyList<SeedBooking> Bookings);

public static class DemoData
{
    public const string Confirmed = "Confirmed";

    public const string Cancelled = "Cancelled";

    private const int RandomSeed = 20260926;

    /// <summary>
    /// Builds matching deterministic event and booking records relative to the supplied date.
    /// </summary>
    public static DemoDataSet Generate(DateTimeOffset now)
    {
        var random = new Random(RandomSeed);
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        string[] categories = ["Music", "Tech", "Sports", "Comedy", "Workshop"];
        string[] cities = ["Bengaluru", "Delhi", "Mumbai", "Chennai", "Hyderabad"];
        string[] subjects = ["Indie Night", "Cloud Summit", "City Marathon", "Stand-up Showcase",
            "Design Lab", "Jazz Evening", "AI Meetup", "Football Final", "Comedy Club", "Pottery Studio"];

        var events = new List<SeedEvent>(40);
        for (var index = 1; index <= 40; index++)
        {
            var upcoming = index <= 30;
            var startsAt = upcoming
                ? today.AddDays(2 + ((index * 7) % 59)).AddHours(10 + (index % 9))
                : today.AddDays(-(15 + ((index * 13) % 165))).AddHours(11 + (index % 8));
            events.Add(new SeedEvent(index, $"{subjects[(index - 1) % subjects.Length]} {index}",
                $"A practical {categories[(index - 1) % categories.Length].ToLowerInvariant()} event for the EventHub demo.",
                categories[(index - 1) % categories.Length], $"{cities[(index - 1) % cities.Length]} Arena {1 + index % 4}",
                cities[(index - 1) % cities.Length], startsAt, index % 8 == 0 ? 0 : random.Next(5, 101) * 50m,
                index == 1 ? 20 : random.Next(120, 301), index % 2 == 0 ? DemoUsers.Organizer2 : DemoUsers.Organizer,
                today.AddDays(-180 + index)));
        }

        var bookings = new List<SeedBooking>(300);
        for (var index = 1; index <= 300; index++)
        {
            var eventId = index <= 10 ? 1 : random.Next(2, 41);
            var quantity = index <= 10 ? 2 : random.Next(1, 5);
            var status = index > 10 && index % 10 == 0 ? Cancelled : Confirmed;
            bookings.Add(new SeedBooking(DeterministicGuid(index), eventId,
                index % 2 == 0 ? DemoUsers.Attendee2 : DemoUsers.Attendee, quantity, status,
                today.AddDays(-random.Next(0, 181)).AddMinutes(index)));
        }

        return new DemoDataSet(events, bookings);
    }

    /// <summary>
    /// Builds a stable booking identifier from its sequence number and the fixed seed.
    /// </summary>
    private static Guid DeterministicGuid(int value)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        BitConverter.GetBytes(RandomSeed).CopyTo(bytes, 4);
        bytes[15] = 0x42;
        return new Guid(bytes);
    }
}
