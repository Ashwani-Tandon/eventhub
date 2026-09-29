// Holds the local model address, name, and whole-chat waiting budget.
// Startup validation catches missing or unsafe settings before the service accepts chat requests.
using System.ComponentModel.DataAnnotations;
namespace Agent.Infrastructure;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";
    [Required] public string Endpoint { get; init; } = "http://localhost:11434";
    [Required] public string Model { get; init; } = "qwen2.5:3b";
    // This is the budget for queueing, model turns and tools together, not 120 seconds per model turn.
    [Range(120, 120)] public int TimeoutSeconds { get; init; } = 120;
}
