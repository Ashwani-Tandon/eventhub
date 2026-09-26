// Registers Catalog's SQL adapters and applies migrations before deterministic development seeding.
// The API composition root calls these methods while Infrastructure owns all EF Core details.
using Catalog.Application;
using Catalog.Domain;
using EventHub.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Catalog.Infrastructure;

public static class CatalogRegistration
{
    public static void AddCatalogInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddSqlServerDbContext<CatalogDbContext>("catalogdb", configureDbContextOptions: options =>
            options.UseSqlServer(sql => sql.CommandTimeout(15)));
        builder.Services.AddScoped<CatalogRepository>();
        builder.Services.AddScoped<IEventRepository>(services => services.GetRequiredService<CatalogRepository>());
        builder.Services.AddScoped<IReservationRepository>(services => services.GetRequiredService<CatalogRepository>());
        builder.Services.AddScoped<IUnitOfWork>(services => services.GetRequiredService<CatalogRepository>());
        builder.Services.AddScoped<IEventQueries, EventQueries>();
    }

    public static async Task InitializeCatalogAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        if (await db.Events.AnyAsync(cancellationToken)) return;

        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var data = DemoData.Generate(clock.GetUtcNow());
        var confirmedSeats = data.Bookings.Where(x => x.Status == DemoData.Confirmed)
            .GroupBy(x => x.EventId).ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity));
        var entities = data.Events.Select(seed => Event.Seed(seed.Id, seed.Title, seed.Description,
            seed.Category, seed.Venue, seed.City, seed.StartsAt, seed.Price, seed.Capacity,
            confirmedSeats.GetValueOrDefault(seed.Id), seed.OrganizerId, seed.CreatedAt)).ToList();
        db.Events.AddRange(entities);
        await db.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < entities.Count; index++)
        {
            if (entities[index].Id != data.Events[index].Id)
                throw new InvalidOperationException("Catalog seed event ids no longer match the shared booking seed.");
        }

        db.SeatReservations.AddRange(data.Bookings.Where(x => x.Status == DemoData.Confirmed)
            .Select(x => SeatReservation.Hold(x.Id, x.EventId, x.AttendeeId, x.Quantity, x.BookedAt)));
        await db.SaveChangesAsync(cancellationToken);
    }
}
