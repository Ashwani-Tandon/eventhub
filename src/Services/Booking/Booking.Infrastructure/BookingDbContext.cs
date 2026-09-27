// Maps bookings and their pre-side-effect idempotency claims to Booking's SQL database.
// Constraints protect purchase facts, lifecycle state, and unique request/reservation identities.
using Booking.Domain;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure;

/// <summary>Owns bookingdb, including durable request claims, without referencing Catalog persistence.</summary>
public sealed class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Booking> Bookings => Set<Domain.Booking>();

    public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();

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
        booking.Property(x => x.IdempotencyKey).HasMaxLength(200);
        booking.HasIndex(x => new { x.UserId, x.IdempotencyKey }).IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");
        booking.HasIndex(x => new { x.UserId, x.CreatedAt });
        booking.HasIndex(x => x.OrganizerId);
        booking.ToTable("Bookings", table =>
        {
            table.HasCheckConstraint("CK_Bookings_Quantity", "[Quantity] BETWEEN 1 AND 10");
            table.HasCheckConstraint("CK_Bookings_Price", "[UnitPrice] >= 0 AND [Total] = [Quantity] * [UnitPrice]");
            table.HasCheckConstraint("CK_Bookings_Status", "[Status] IN ('Confirmed', 'Cancelled')");
            table.HasCheckConstraint("CK_Bookings_Pending", "[SeatReleasePending] = 0 OR [Status] = 'Cancelled'");
        });

        var request = modelBuilder.Entity<BookingRequest>();
        request.HasKey(x => x.Id);
        request.Property(x => x.Id).ValueGeneratedOnAdd();
        request.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        request.Property(x => x.State).HasMaxLength(20).IsRequired();
        request.HasIndex(x => new { x.UserId, x.IdempotencyKey }).IsUnique();
        request.HasIndex(x => x.BookingId).IsUnique().HasFilter("[BookingId] IS NOT NULL");
        request.HasOne<Domain.Booking>().WithMany().HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
        request.ToTable("BookingRequests", table =>
        {
            table.HasCheckConstraint("CK_BookingRequests_Quantity", "[Quantity] BETWEEN 1 AND 10");
            table.HasCheckConstraint("CK_BookingRequests_State", "[State] IN ('Processing', 'Completed')");
            table.HasCheckConstraint("CK_BookingRequests_Completion",
                "([State] = 'Processing' AND [BookingId] IS NULL) OR ([State] = 'Completed' AND [BookingId] IS NOT NULL)");
        });
    }
}
