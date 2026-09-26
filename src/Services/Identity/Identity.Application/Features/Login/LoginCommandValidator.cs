// Checks login input before looking up a user.
// The mediator runs these rules before dispatching the request to its handler.
using FluentValidation;

namespace Identity.Application.Features.Login;

/// <summary>
/// Checks login input before looking up a user. The mediator runs these rules before dispatching the request to its handler.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Registers this request's field rules so malformed input is rejected before its handler executes.
    /// </summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty();
    }
}
