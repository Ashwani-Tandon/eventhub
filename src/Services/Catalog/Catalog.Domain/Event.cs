// Models an event and protects capacity, scheduling, and deletion invariants.
// Command handlers call these rules before Infrastructure persists the aggregate.
using EventHub.BuildingBlocks.Domain;
using EventHub.BuildingBlocks.Results;
using System.Diagnostics.CodeAnalysis;

namespace Catalog.Domain;

[SuppressMessage("Naming", "CA1716", Justification = "The specification names this domain aggregate Event.")]
public sealed class Event : Entity<int>
{
    private Event() : base(0) { }

    private Event(int id, string title, string description, string category, string venue, string city,
        DateTimeOffset startsAt, decimal price, int capacity, int seatsBooked, Guid organizerId,
        DateTimeOffset createdAt) : base(id)
    {
        Title = title;
        Description = description;
        Category = category;
        Venue = venue;
        City = city;
        StartsAt = startsAt;
        Price = price;
        Capacity = capacity;
        SeatsBooked = seatsBooked;
        OrganizerId = organizerId;
        CreatedAt = createdAt;
    }

    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string Category { get; private set; } = "";
    public string Venue { get; private set; } = "";
    public string City { get; private set; } = "";
    public DateTimeOffset StartsAt { get; private set; }
    public decimal Price { get; private set; }
    public int Capacity { get; private set; }
    public int SeatsBooked { get; private set; }
    public Guid OrganizerId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Event> Create(string title, string description, string category, string venue,
        string city, DateTimeOffset startsAt, decimal price, int capacity, Guid organizerId,
        DateTimeOffset now)
    {
        var validation = Validate(title, category, startsAt, price, capacity, 0, now, true);
        return validation.IsFailure
            ? Result<Event>.Failure(validation.Error!)
            : Result<Event>.Success(new Event(0, title.Trim(), description.Trim(), category, venue.Trim(),
                city.Trim(), startsAt, price, capacity, 0, organizerId, now));
    }

    public static Event Seed(int expectedId, string title, string description, string category, string venue,
        string city, DateTimeOffset startsAt, decimal price, int capacity, int seatsBooked,
        Guid organizerId, DateTimeOffset createdAt) =>
        new(0, title, description, category, venue, city, startsAt, price, capacity,
            seatsBooked, organizerId, createdAt);

    public Result Update(string title, string description, string category, string venue, string city,
        DateTimeOffset startsAt, decimal price, int capacity, DateTimeOffset now)
    {
        var dateChanged = startsAt != StartsAt;
        var validation = Validate(title, category, startsAt, price, capacity, SeatsBooked, now, dateChanged);
        if (validation.IsFailure)
        {
            return validation;
        }

        Title = title.Trim();
        Description = description.Trim();
        Category = category;
        Venue = venue.Trim();
        City = city.Trim();
        StartsAt = startsAt;
        Price = price;
        Capacity = capacity;
        return Result.Success();
    }

    public Result CanBeDeleted() => SeatsBooked > 0 ? Result.Failure(EventErrors.HasBookings) : Result.Success();

    private static Result Validate(string title, string category, DateTimeOffset startsAt, decimal price,
        int capacity, int seatsBooked, DateTimeOffset now, bool requireFutureDate)
    {
        if (string.IsNullOrWhiteSpace(title)) return Result.Failure(EventErrors.Invalid("Title", "Title is required."));
        if (!Categories.IsKnown(category)) return Result.Failure(EventErrors.Invalid("Category", "Choose a known category."));
        if (price < 0) return Result.Failure(EventErrors.Invalid("Price", "Price must be zero or greater."));
        if (capacity is < 1 or > 10000) return Result.Failure(EventErrors.Invalid("Capacity", "Capacity must be between 1 and 10000."));
        if (capacity < seatsBooked) return Result.Failure(EventErrors.Invalid("Capacity", "Capacity cannot be below seats already booked."));
        if (requireFutureDate && startsAt <= now) return Result.Failure(EventErrors.Invalid("StartsAt", "Start time must be in the future."));
        return Result.Success();
    }
}
