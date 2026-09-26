// Checks that the demo message is present and at most 20 characters.
// The mediator validation behavior runs these rules before allowing the handler to execute.
using FluentValidation;

namespace Identity.Api.Debugging;

/// <summary>
/// Checks that the demo message is present and at most 20 characters. The mediator validation behavior runs these rules before allowing the handler to execute.
/// </summary>
public sealed class EchoCommandValidator : AbstractValidator<EchoCommand>
{
    /// <summary>
    /// Registers this request's field rules so malformed input is rejected before its handler executes.
    /// </summary>
    public EchoCommandValidator()
    {
        RuleFor(static command => command.Message)
            .NotEmpty()
            .MaximumLength(20);
    }
}
