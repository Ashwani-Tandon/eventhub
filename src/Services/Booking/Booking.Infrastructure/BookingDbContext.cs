// Maps Booking's purchase snapshots and cancellation recovery flag to its own SQL database.
// Database constraints protect stored quantities, prices, statuses, and unique reservation identities.
using Booking.Domain;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure;

/// <summary>Owns bookingdb without referencing Catalog tables or its DbContext.</summary>
public sealed class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Booking> Bookings => Set<Domain.Booking>();

    /// <summary>Configures purchase fields and the integrity constraints beneath the Domain rules.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var booking = modelBuilder.Entity<Domain.Booking>();
        booking.HasKey(x => x.Id);
        booking.Property(x => x.Id).ValueGeneratedOnAdd();
        booking.Property(x => x.EventTitle).HasMaxLength(120).IsRequired();
        booking.Property(x => x.UnitPrice).HasPrecision(10, 2);
        booking.Property(x => x.Total).HasPrecision(12, 2);
        booking.Property(x => x.Status).HasMaxLength(20).IsRequired();
        booking.Property(x => x.PaymentRef).HasMaxLength(40).IsRequired();
        booking.HasIndex(x => x.ReservationId).IsUnique();
        booking.HasIndex(x => new { x.UserId, x.CreatedAt });
        booking.HasIndex(x => x.OrganizerId);
        booking.ToTable("Bookings", table =>
        {
            table.HasCheckConstraint("CK_Bookings_Quantity", "[Quantity] BETWEEN 1 AND 10");
            table.HasCheckConstraint("CK_Bookings_Price", "[UnitPrice] >= 0 AND [Total] = [Quantity] * [UnitPrice]");
            table.HasCheckConstraint("CK_Bookings_Status", "[Status] IN ('Confirmed', 'Cancelled')");
            table.HasCheckConstraint("CK_Bookings_Pending", "[SeatReleasePending] = 0 OR [Status] = 'Cancelled'");
        });
    }
}
