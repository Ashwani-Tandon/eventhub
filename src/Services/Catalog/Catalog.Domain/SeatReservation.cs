// Records the idempotency identity and lifecycle of a seat hold.
// Catalog stores one row per booking reservation so replayed operations change seats only once.
using EventHub.BuildingBlocks.Domain;

namespace Catalog.Domain;

public static class ReservationStatuses
{
    public const string Held = "Held";
    public const string Released = "Released";
}

public sealed class SeatReservation : Entity<Guid>
{
    private SeatReservation() : base(Guid.Empty) { }

    private SeatReservation(Guid id, int eventId, Guid userId, int quantity, string status,
        DateTimeOffset createdAt, DateTimeOffset? releasedAt) : base(id)
    {
        EventId = eventId;
        UserId = userId;
        Quantity = quantity;
        Status = status;
        CreatedAt = createdAt;
        ReleasedAt = releasedAt;
    }

    public int EventId { get; private set; }
    public Guid UserId { get; private set; }
    public int Quantity { get; private set; }
    public string Status { get; private set; } = ReservationStatuses.Held;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }

    public static SeatReservation Hold(Guid id, int eventId, Guid userId, int quantity, DateTimeOffset now) =>
        new(id, eventId, userId, quantity, ReservationStatuses.Held, now, null);

    public void Release(DateTimeOffset now)
    {
        if (Status == ReservationStatuses.Released) return;
        Status = ReservationStatuses.Released;
        ReleasedAt = now;
    }
}
