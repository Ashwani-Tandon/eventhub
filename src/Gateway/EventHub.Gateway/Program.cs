// Routes public API requests to Aspire-discovered services through YARP.
// Rejects Catalog internal paths before proxying external traffic.
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.Use(async (context, next) =>
{
// A customer must buy through Booking. Block public access to the routes that directly change seats.
    if (context.Request.Path.StartsWithSegments(
            "/catalog/internal",
            StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

// Pass other requests to the forwarding tool, YARP, which sends them to the service named in our settings.
    await next(context);
});

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
