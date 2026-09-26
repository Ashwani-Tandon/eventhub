// Checks login input before looking up a user.
// The mediator runs these rules before dispatching the request to its handler.
using FluentValidation;
namespace Identity.Application.Features.Login;
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty();
    }
}
