// Models an event and protects capacity, scheduling, and deletion invariants.
// Command handlers call these rules before Infrastructure persists the aggregate.
using System.Diagnostics.CodeAnalysis;
using EventHub.BuildingBlocks.Domain;
using EventHub.BuildingBlocks.Results;

namespace Catalog.Domain;

[SuppressMessage("Naming", "CA1716", Justification = "The specification names this domain aggregate Event.")]
/// <summary>
/// Models an event and protects capacity, scheduling, and deletion invariants. Command handlers call these rules before Infrastructure persists the aggregate.
/// </summary>
public sealed class Event : Entity<int>
{
    /// <summary>
    /// Reconstructs stored event fields; EF Core uses the empty constructor while public creation checks Domain rules.
    /// </summary>
    private Event() : base(0)
    {
    }

    /// <summary>
    /// Reconstructs stored event fields; EF Core uses the empty constructor while public creation checks Domain rules.
    /// </summary>
    private Event(
        int id,
        string title,
        string description,
        string category,
        string venue,
        string city,
        DateTimeOffset startsAt,
        decimal price,
        int capacity,
        int seatsBooked,
        Guid organizerId,
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

    /// <summary>
    /// Validates new event rules and creates an aggregate owned by the authenticated organizer.
    /// </summary>
    public static Result<Event> Create(
        string title,
        string description,
        string category,
        string venue,
        string city,
        DateTimeOffset startsAt,
        decimal price,
        int capacity,
        Guid organizerId,
        DateTimeOffset now)
    {
        var validation = Validate(title, category, startsAt, price, capacity, 0, now, true);
        return validation.IsFailure
            ? Result<Event>.Failure(validation.Error!)
            : Result<Event>.Success(new Event(0, title.Trim(), description.Trim(), category, venue.Trim(),
                city.Trim(), startsAt, price, capacity, 0, organizerId, now));
    }

    /// <summary>
    /// Creates a development seed aggregate, including booked seats and historical dates.
    /// </summary>
    public static Event Seed(
        int expectedId,
        string title,
        string description,
        string category,
        string venue,
        string city,
        DateTimeOffset startsAt,
        decimal price,
        int capacity,
        int seatsBooked,
        Guid organizerId,
        DateTimeOffset createdAt)
    {
        return new(0, title, description, category, venue, city, startsAt, price, capacity,
            seatsBooked, organizerId, createdAt);
    }

    /// <summary>
    /// Applies event changes only when capacity and changed start-time rules remain valid.
    /// </summary>
    public Result Update(
        string title,
        string description,
        string category,
        string venue,
        string city,
        DateTimeOffset startsAt,
        decimal price,
        int capacity,
        DateTimeOffset now)
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

    /// <summary>
    /// Refuses deletion while this event has booked seats.
    /// </summary>
    public Result CanBeDeleted()
    {
        return SeatsBooked > 0 ? Result.Failure(EventErrors.HasBookings) : Result.Success();
    }

    /// <summary>
    /// Checks the domain invariants that must hold regardless of the request entry point.
    /// </summary>
    private static Result Validate(
        string title,
        string category,
        DateTimeOffset startsAt,
        decimal price,
        int capacity,
        int seatsBooked,
        DateTimeOffset now,
        bool requireFutureDate)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure(EventErrors.Invalid("Title", "Title is required."));
        }

        if (!Categories.IsKnown(category))
        {
            return Result.Failure(EventErrors.Invalid("Category", "Choose a known category."));
        }

        if (price < 0)
        {
            return Result.Failure(EventErrors.Invalid("Price", "Price must be zero or greater."));
        }

        if (capacity is < 1 or > 10000)
        {
            return Result.Failure(EventErrors.Invalid("Capacity", "Capacity must be between 1 and 10000."));
        }

        if (capacity < seatsBooked)
        {
            return Result.Failure(EventErrors.Invalid("Capacity", "Capacity cannot be below seats already booked."));
        }

        if (requireFutureDate && startsAt <= now)
        {
            return Result.Failure(EventErrors.Invalid("StartsAt", "Start time must be in the future."));
        }

        return Result.Success();
    }
}
