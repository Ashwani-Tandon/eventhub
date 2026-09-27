// Stores a confirmed purchase and the Catalog event snapshot taken at purchase time.
// Cancellation persists a pending seat-release flag so recovery can resume after Catalog becomes available.
using EventHub.BuildingBlocks.Domain;
using EventHub.BuildingBlocks.Results;

namespace Booking.Domain;

/// <summary>Owns purchase facts and cancellation rules; persistence and HTTP stay outside this aggregate.</summary>
public sealed class Booking : Entity<int>
{
    // EF Core materializes saved bookings through this constructor.
    private Booking() : base(0)
    {
    }

    public Guid UserId { get; private set; }
    public int EventId { get; private set; }
    public string EventTitle { get; private set; } = "";
    public DateTimeOffset EventStartsAt { get; private set; }
    public Guid OrganizerId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Total { get; private set; }
    public string Status { get; private set; } = BookingStatus.Confirmed;
    public string PaymentRef { get; private set; } = "";
    public Guid ReservationId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public bool SeatReleasePending { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a confirmed booking with immutable purchase-time event and price facts.</summary>
    public static Result<Booking> Create(
        Guid userId,
        int eventId,
        string eventTitle,
        DateTimeOffset eventStartsAt,
        Guid organizerId,
        int quantity,
        decimal unitPrice,
        string paymentRef,
        Guid reservationId,
        string? idempotencyKey,
        DateTimeOffset now)
    {
        if (quantity is < 1 or > 10)
        {
            return Result<Booking>.Failure(BookingErrors.InvalidQuantity);
        }

        if (eventStartsAt <= now)
        {
            return Result<Booking>.Failure(BookingErrors.EventStarted);
        }

        return Result<Booking>.Success(Build(userId, eventId, eventTitle, eventStartsAt,
            organizerId, quantity, unitPrice, paymentRef, reservationId, idempotencyKey, now));
    }

    /// <summary>Restores deterministic demo purchases, including historical and already-cancelled bookings.</summary>
    public static Booking Seed(
        Guid userId,
        int eventId,
        string eventTitle,
        DateTimeOffset eventStartsAt,
        Guid organizerId,
        int quantity,
        decimal unitPrice,
        string paymentRef,
        Guid reservationId,
        string status,
        DateTimeOffset createdAt)
    {
        var booking = Build(userId, eventId, eventTitle, eventStartsAt, organizerId,
            quantity, unitPrice, paymentRef, reservationId, null, createdAt);
        booking.Status = status;
        return booking;
    }

    /// <summary>Starts cancellation once; callers recognize completed and pending replays before invoking this rule.</summary>
    public Result BeginCancellation(DateTimeOffset now)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return Result.Failure(BookingErrors.NotConfirmed);
        }

        if (EventStartsAt <= now)
        {
            return Result.Failure(BookingErrors.EventStarted);
        }

        // Record that the user's cancellation is accepted, but the seats still need to be returned.
        // If the app stops here, the next Cancel request can finish that remaining work.
        Status = BookingStatus.Cancelled;
        SeatReleasePending = true;
        return Result.Success();
    }

    /// <summary>Clears the recovery flag only after Catalog acknowledges the idempotent release.</summary>
    public void CompleteSeatRelease()
    {
        SeatReleasePending = false;
    }

    /// <summary>Copies purchase facts for both normal creation and development seeding.</summary>
    private static Booking Build(
        Guid userId,
        int eventId,
        string eventTitle,
        DateTimeOffset eventStartsAt,
        Guid organizerId,
        int quantity,
        decimal unitPrice,
        string paymentRef,
        Guid reservationId,
        string? idempotencyKey,
        DateTimeOffset createdAt)
    {
        return new Booking
        {
            UserId = userId,
            EventId = eventId,
            EventTitle = eventTitle,
            EventStartsAt = eventStartsAt,
            OrganizerId = organizerId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Total = quantity * unitPrice,
            PaymentRef = paymentRef,
            ReservationId = reservationId,
            IdempotencyKey = idempotencyKey,
            CreatedAt = createdAt
        };
    }
}
