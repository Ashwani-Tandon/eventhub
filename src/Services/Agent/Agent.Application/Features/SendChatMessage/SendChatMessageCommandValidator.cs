// Rejects empty, oversized, or privileged-role histories before contacting Ollama.
// Bounded text protects this small local model, and only the server may create system/tool messages.
using Agent.Application.Contracts;
using FluentValidation;
namespace Agent.Application.Features.SendChatMessage;

public sealed class SendChatMessageCommandValidator : AbstractValidator<SendChatMessageCommand>
{
    /// <summary>Checks history structure and limits while allowing normal multi-turn user/assistant conversations.</summary>
    public SendChatMessageCommandValidator()
    {
        RuleFor(x => x.Messages).Cascade(CascadeMode.Stop).NotEmpty()
            .Must(x => x!.Count <= 30).WithMessage("Send at most 30 messages.")
            .Must(x => x!.All(m => m is not null)).WithMessage("Messages cannot be null.")
            .Must(x => x![^1].Role == HistoryRoles.User).WithMessage("The last message must be from the user.")
            .Must(x => x!.Sum(m => m.Content?.Length ?? 0) <= 32000).WithMessage("Conversation text must not exceed 32000 characters.");
        RuleForEach(x => x.Messages).ChildRules(turn =>
        {
            turn.RuleFor(x => x.Role).Must(x => x is HistoryRoles.User or HistoryRoles.Assistant)
                .WithMessage("Role must be user or assistant.");
            turn.RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
        });
    }
}
