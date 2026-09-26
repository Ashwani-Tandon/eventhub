// Checks the role vocabulary and user identifier.
// The mediator runs these rules before dispatching the request to its handler.
using FluentValidation;
using Identity.Domain;
namespace Identity.Application.Features.ChangeUserRole;
public sealed class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Role).Must(Roles.IsValid).WithMessage("Choose Attendee, Organizer, or Admin.");
    }
}
