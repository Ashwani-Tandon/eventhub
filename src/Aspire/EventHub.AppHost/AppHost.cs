// Starts SQL, backend services, and the Angular dev server through Aspire.
// Supplies secret parameters and discovery references to the services that need them.
var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var jwtKey = builder.AddParameter("jwt-key", secret: true);
var bookingServiceKey = builder.AddParameter("booking-service-key", secret: true);

// Keep saved users, events, and bookings when the app stops and starts again.
// Each service has its own database, even though this computer runs them on one SQL server.
var sql = builder.AddSqlServer("sql", password: sqlPassword)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var identityDatabase = sql.AddDatabase("identitydb");
var catalogDatabase = sql.AddDatabase("catalogdb");
var bookingDatabase = sql.AddDatabase("bookingdb");

var identity = builder.AddProject<Projects.Identity_Api>("identity")
    .WithReference(identityDatabase)
    .WaitFor(identityDatabase)
    .WithEnvironment("Jwt__Key", jwtKey);

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog")
    .WithReference(catalogDatabase)
    .WaitFor(catalogDatabase)
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("BookingService__Key", bookingServiceKey);

// Tell Booking how to reach Catalog, and wait until Catalog is ready before starting Booking.
// On its first start, Booking needs Catalog's event details to create the demo bookings.
var booking = builder.AddProject<Projects.Booking_Api>("booking")
    .WithReference(bookingDatabase)
    .WaitFor(bookingDatabase)
    .WithReference(catalog)
    .WaitFor(catalog)
    .WithEnvironment("Jwt__Key", jwtKey)
    .WithEnvironment("BookingService__Key", bookingServiceKey);

var agent = builder.AddProject<Projects.Agent_Api>("agent")
    .WithReference(catalog)
    .WithReference(booking)
    .WaitFor(catalog)
    .WaitFor(booking)
    .WithEnvironment("Jwt__Key", jwtKey);

var gateway = builder.AddProject<Projects.EventHub_Gateway>("gateway")
    .WithReference(identity)
    .WithReference(catalog)
    .WithReference(booking)
    .WithReference(agent)
    .WaitFor(identity)
    .WaitFor(catalog)
    .WaitFor(booking)
    .WaitFor(agent)
    .WithExternalHttpEndpoints();

// Run the existing Angular start script once Gateway is ready so login can work immediately.
// Angular owns port 4200 directly; disabling Aspire's port proxy avoids two listeners on that port.
builder.AddExecutable("web", "npm", "../../../web", "run", "start")
    .WithHttpEndpoint(port: 4200, targetPort: 4200, isProxied: false)
    .WithExternalHttpEndpoints()
    .WaitFor(gateway);

builder.Build().Run();
