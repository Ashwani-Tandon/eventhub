// Maps Identity's user aggregate to its own SQL database.
// This adapter connects the application ports to the hosting infrastructure.
using Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure;

/// <summary>
/// Maps Identity's user aggregate to its own SQL database. This adapter connects the application ports to the hosting infrastructure.
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Maps Users with a unique email index and allowed-role constraint so SQL also enforces identity integrity.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.HasKey(x => x.Id);
        user.Property(x => x.Email).HasMaxLength(256).IsRequired();
        user.HasIndex(x => x.Email).IsUnique();
        user.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        user.Property(x => x.PasswordHash).IsRequired();
        user.Property(x => x.Role).HasMaxLength(20).IsRequired();
        user.ToTable("Users", table => table.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Attendee', 'Organizer', 'Admin')"));
    }
}
