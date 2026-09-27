// Registers Booking's persistence, payment, and Catalog adapters and initializes its development data.
// Seeding reads public Catalog snapshots through HTTP; it never opens Catalog's database.
using Booking.Application.Ports;
using EventHub.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Booking.Infrastructure;

/// <summary>Infrastructure composition and migration-first development seeding for bookingdb.</summary>
public static class BookingRegistration
{
    /// <summary>Wires local SQL ports and a bounded Catalog client with discovery and caller-token forwarding.</summary>
    public static void AddBookingInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddSqlServerDbContext<BookingDbContext>("bookingdb", configureDbContextOptions: options =>
            options.UseSqlServer(sql => sql.CommandTimeout(15)));
        builder.Services.AddScoped<BookingRepository>();
        builder.Services.AddScoped<IBookingRepository>(services => services.GetRequiredService<BookingRepository>());
        builder.Services.AddScoped<IBookingRequestRepository>(services => services.GetRequiredService<BookingRepository>());
        builder.Services.AddScoped<IUnitOfWork>(services => services.GetRequiredService<BookingRepository>());
        builder.Services.AddScoped<IBookingQueries, BookingQueries>();
        builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
        builder.Services.AddOptions<BookingServiceOptions>()
            .BindConfiguration(BookingServiceOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddTransient<ForwardTokenHandler>();
        builder.Services.AddHttpClient<ICatalogClient, CatalogHttpClient>(client =>
        {
            client.BaseAddress = new Uri("https+http://catalog");
            // Step-17 replaces this total bound with the shared retry/breaker pipeline.
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddHttpMessageHandler<ForwardTokenHandler>();
    }

    /// <summary>Migrates first, then seeds the shared purchases only when Booking's table is empty.</summary>
    public static async Task InitializeBookingAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        if (await db.Bookings.AnyAsync(cancellationToken))
        {
            return;
        }

        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogClient>();
        var data = DemoData.Generate(clock.GetUtcNow());
        var snapshots = new Dictionary<int, CatalogEvent>();
        foreach (var seedEvent in data.Events)
        {
            var result = await catalog.GetEventAsync(seedEvent.Id, cancellationToken);
            if (result.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Cannot seed Booking because Catalog event {seedEvent.Id} could not be read: {result.Error!.Code}.");
            }

            // Catalog may have been seeded on an earlier day; copy its persisted facts rather than regenerated dates.
            snapshots.Add(seedEvent.Id, result.Value);
        }

        foreach (var purchase in data.Bookings)
        {
            var eventItem = snapshots[purchase.EventId];
            var booking = Domain.Booking.Seed(
                purchase.AttendeeId, eventItem.Id, eventItem.Title, eventItem.StartsAt, eventItem.OrganizerId,
                purchase.Quantity, eventItem.Price, $"PAY-{purchase.Id.ToString("N")[..8].ToUpperInvariant()}",
                purchase.Id, purchase.Status, purchase.BookedAt);
            db.Bookings.Add(booking);
        }

        // One save transaction prevents a partial dataset if a seed insert fails.
        await db.SaveChangesAsync(cancellationToken);
    }
}
