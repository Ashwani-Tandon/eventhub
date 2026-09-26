// Rejects malformed registration input before persistence.
// The mediator runs these rules before dispatching the request to its handler.
using FluentValidation;

namespace Identity.Application.Features.RegisterUser;

/// <summary>
/// Rejects malformed registration input before persistence. The mediator runs these rules before dispatching the request to its handler.
/// </summary>
public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    /// <summary>
    /// Registers this request's field rules so malformed input is rejected before its handler executes.
    /// </summary>
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
