// Composes Identity's use cases, authentication, and SQL adapters.
// Development startup applies migrations before creating the demo users.
using EventHub.BuildingBlocks.Messaging;
using Identity.Api;
using Identity.Api.Debugging;
using Identity.Application.Features.Login;
using Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddEventHubAuth();
builder.AddIdentityInfrastructure();
builder.Services.AddMediator(typeof(EchoCommand).Assembly, typeof(LoginCommand).Assembly);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapIdentityEndpoints();

if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeIdentityAsync(app.Lifetime.ApplicationStopping);
    app.MapDebugEndpoints();
}

app.MapDefaultEndpoints();

app.Run();
