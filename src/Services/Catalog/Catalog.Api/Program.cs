// Composes Catalog's use cases, authentication, internal credential guard, and SQL adapters.
// Development startup migrates and seeds Catalog before requests enter the CQRS pipeline.
using Catalog.Api;
using Catalog.Application.Features.CreateEvent;
using Catalog.Infrastructure;
using EventHub.BuildingBlocks.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddEventHubAuth();
builder.AddCatalogInfrastructure();
builder.Services.AddCatalogApi(builder.Configuration);
builder.Services.AddMediator(typeof(CreateEventCommand).Assembly);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapCatalogEndpoints();

if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeCatalogAsync(app.Lifetime.ApplicationStopping);
}

app.MapDefaultEndpoints();

app.Run();
