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
