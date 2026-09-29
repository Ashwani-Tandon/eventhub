// Composes the authenticated read-only assistant with its mediator, service tools and local Ollama model.
// The Agent owns no database; all event and booking facts arrive through the existing APIs.
using Agent.Api;
using Agent.Application.Features.SendChatMessage;
using Agent.Infrastructure;
using EventHub.BuildingBlocks.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddEventHubAuth();
builder.AddAgentInfrastructure();
builder.Services.AddMediator(typeof(SendChatMessageCommand).Assembly);
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseAgentAvailabilityHeaders();
app.MapAgentEndpoints();
app.MapDefaultEndpoints();
app.Run();
