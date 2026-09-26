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
