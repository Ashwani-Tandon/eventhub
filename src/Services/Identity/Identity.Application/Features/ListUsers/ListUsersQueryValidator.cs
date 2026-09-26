// Registers the parameterless admin query with validation.
// The mediator runs these rules before dispatching the request to its handler.
using FluentValidation;
namespace Identity.Application.Features.ListUsers;
public sealed class ListUsersQueryValidator : AbstractValidator<ListUsersQuery>;
