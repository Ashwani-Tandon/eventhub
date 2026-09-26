// Rejects malformed registration input before persistence.
// The mediator runs these rules before dispatching the request to its handler.
using FluentValidation;
namespace Identity.Application.Features.RegisterUser;
public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
