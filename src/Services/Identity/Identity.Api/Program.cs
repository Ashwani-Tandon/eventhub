using EventHub.BuildingBlocks.Messaging;
using Identity.Api.Debugging;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddMediator(typeof(EchoCommand).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapDebugEndpoints();
}

app.MapDefaultEndpoints();

app.Run();
// Composes the Identity API and registers shared hosting and mediator services.
// During Development it exposes the echo example so the pipeline can be explored before real use cases.
