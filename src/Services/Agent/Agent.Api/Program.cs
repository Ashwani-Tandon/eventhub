// Starts this service's authentication, hosting defaults, and health endpoints.
// Business use cases are added in the execution-plan step for this service.
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddEventHubAuth();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

app.Run();
