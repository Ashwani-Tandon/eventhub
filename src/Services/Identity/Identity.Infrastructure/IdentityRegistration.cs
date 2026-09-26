// Registers SQL adapters and migrates before development seeding.
// This adapter connects the application ports to the hosting infrastructure.
using EventHub.SeedData;
using Identity.Application;
using Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace Identity.Infrastructure;
public static class IdentityRegistration
{
    public static void AddIdentityInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddSqlServerDbContext<IdentityDbContext>("identitydb", configureDbContextOptions: options =>
            options.UseSqlServer(sql => sql.CommandTimeout(15)));
        builder.Services.AddScoped<UserRepository>();
        builder.Services.AddScoped<IUserRepository>(s => s.GetRequiredService<UserRepository>());
        builder.Services.AddScoped<IUserQueries>(s => s.GetRequiredService<UserRepository>());
        builder.Services.AddScoped<IUnitOfWork>(s => s.GetRequiredService<UserRepository>());
        builder.Services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
    }
    public static async Task InitializeIdentityAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var entries = new (Guid Id, string Email, string Name, string Role)[]
        {
            (DemoUsers.Admin, "admin@demo.com", "Demo Admin", Roles.Admin),
            (DemoUsers.Organizer, "organizer@demo.com", "Demo Organizer", Roles.Organizer),
            (DemoUsers.Organizer2, "organizer2@demo.com", "Demo Organizer 2", Roles.Organizer),
            (DemoUsers.Attendee, "attendee@demo.com", "Demo Attendee", Roles.Attendee),
            (DemoUsers.Attendee2, "attendee2@demo.com", "Demo Attendee 2", Roles.Attendee)
        };
        foreach (var entry in entries)
        {
            var user = User.Create(entry.Id, entry.Email, entry.Name, hasher.Hash(DemoUsers.Password), clock.GetUtcNow());
            user.ChangeRole(entry.Role);
            db.Users.Add(user);
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
