using FluentValidation;

namespace Identity.Api.Debugging;

public sealed class EchoCommandValidator : AbstractValidator<EchoCommand>
{
    public EchoCommandValidator()
    {
        RuleFor(static command => command.Message)
            .NotEmpty()
            .MaximumLength(20);
    }
}
// Checks that the demo message is present and at most 20 characters.
// The mediator validation behavior runs these rules before allowing the handler to execute.
