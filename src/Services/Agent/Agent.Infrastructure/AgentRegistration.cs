// Wires the chat use case to Ollama and the two existing service APIs.
// Only service reads get the shared HTTP retry pipeline; the model client deliberately has no retry handler.
using System.Threading.RateLimiting;
using Agent.Application.Ports;
using Agent.Application.Tools;
using EventHub.ServiceDefaults;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OllamaSharp;
namespace Agent.Infrastructure;

public static class AgentRegistration
{
    /// <summary>Registers validated model settings, one shared capacity limit, and per-request tools/adapters.</summary>
    public static void AddAgentInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<OllamaOptions>().BindConfiguration(OllamaOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(x => Uri.TryCreate(x.Endpoint, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https",
                "Ollama Endpoint must be an absolute HTTP or HTTPS address.")
            .ValidateOnStart();
        builder.Services.AddScoped<EventHubTools>();
        builder.Services.AddScoped<IAgentChatClient, OllamaChatClient>();
        builder.Services.AddSingleton(_ => new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = 2, QueueLimit = 5, QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));
        builder.Services.AddTransient<ForwardTokenHandler>();
        builder.Services.AddHttpClient<ICatalogApi, CatalogApi>(client => ConfigureServiceClient(client, "https+http://catalog"))
            .AddHttpMessageHandler<ForwardTokenHandler>().AddEventHubResilience("Catalog");
        builder.Services.AddHttpClient<IBookingApi, BookingApi>(client => ConfigureServiceClient(client, "https+http://booking"))
            .AddHttpMessageHandler<ForwardTokenHandler>().AddEventHubResilience("Booking");
        // DI owns this model-only transport and disposes it on shutdown; it never carries a user's JWT.
        builder.Services.AddSingleton(services =>
        {
            var options = services.GetRequiredService<IOptions<OllamaOptions>>().Value;
            return new HttpClient { BaseAddress = new Uri(options.Endpoint), Timeout = Timeout.InfiniteTimeSpan };
        });
        builder.Services.AddChatClient(services =>
        {
            var options = services.GetRequiredService<IOptions<OllamaOptions>>().Value;
            // OllamaSharp speaks Ollama's JSON API. The outer linked token owns the full 120-second conversation budget.
            return new OllamaApiClient(services.GetRequiredService<HttpClient>(), options.Model);
        })
            .UseFunctionInvocation(configure: loop =>
            {
                // One user may request several reads. Run them serially so request context is not used concurrently.
                loop.AllowConcurrentInvocation = false;
                // Prevent a model that keeps requesting tools from looping without producing a final answer.
                loop.MaximumIterationsPerRequest = 8;
            })
            .UseLogging();
    }

    /// <summary>Sets discovery addresses while leaving timeout enforcement to the shared resilience pipeline.</summary>
    private static void ConfigureServiceClient(HttpClient client, string address)
    {
        client.BaseAddress = new Uri(address);
        client.Timeout = Timeout.InfiniteTimeSpan;
    }
}
