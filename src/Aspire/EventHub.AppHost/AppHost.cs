// Starts the SQL resources and service dependency graph through Aspire.
// Supplies secret parameters and discovery references to the services that need them.
var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var jwtKey = builder.AddParameter("jwt-key", secret: true);
var bookingServiceKey = builder.AddParameter("booking-service-key", secret: true);

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

builder.AddProject<Projects.EventHub_Gateway>("gateway")
    .WithReference(identity)
    .WithReference(catalog)
    .WithReference(booking)
    .WithReference(agent)
    .WaitFor(identity)
    .WaitFor(catalog)
    .WaitFor(booking)
    .WaitFor(agent)
    .WithExternalHttpEndpoints();

builder.Build().Run();
