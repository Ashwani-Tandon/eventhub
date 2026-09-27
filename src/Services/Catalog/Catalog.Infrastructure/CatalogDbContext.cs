// Maps Catalog events, row-version tokens, and reservations to the service-owned SQL database.
// Constraints and concurrency metadata form the integrity boundary beneath application rules.
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

/// <summary>
/// Owns Catalog's Events and SeatReservations tables, their relationship, and database constraints on capacity and reservation state.
/// </summary>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();

    public DbSet<SeatReservation> SeatReservations => Set<SeatReservation>();

    /// <summary>
    /// Configures SQL column limits, event-to-reservation ownership, and constraints that keep seat counts between zero and capacity.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var eventItem = modelBuilder.Entity<Event>();
        eventItem.HasKey(x => x.Id);
        eventItem.Property(x => x.Id).ValueGeneratedOnAdd();
        eventItem.Property(x => x.Title).HasMaxLength(120).IsRequired();
        eventItem.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        eventItem.Property(x => x.Category).HasMaxLength(30).IsRequired();
        eventItem.Property(x => x.Venue).HasMaxLength(200).IsRequired();
        eventItem.Property(x => x.City).HasMaxLength(100).IsRequired();
        eventItem.Property(x => x.Price).HasPrecision(10, 2);
        eventItem.Property(x => x.RowVersion).IsRowVersion();
        eventItem.ToTable("Events", table =>
        {
            table.HasCheckConstraint("CK_Events_Price", "[Price] >= 0");
            table.HasCheckConstraint("CK_Events_Capacity", "[Capacity] BETWEEN 1 AND 10000");
            table.HasCheckConstraint("CK_Events_SeatsBooked", "[SeatsBooked] BETWEEN 0 AND [Capacity]");
            table.HasCheckConstraint("CK_Events_Category", "[Category] IN ('Music','Tech','Sports','Comedy','Workshop')");
        });

        var reservation = modelBuilder.Entity<SeatReservation>();
        reservation.HasKey(x => x.Id);
        reservation.Property(x => x.Status).HasMaxLength(20).IsRequired();
        reservation.HasOne<Event>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        reservation.ToTable("SeatReservations", table =>
        {
            table.HasCheckConstraint("CK_SeatReservations_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint("CK_SeatReservations_Status", "[Status] IN ('Held','Released')");
        });
    }
}
