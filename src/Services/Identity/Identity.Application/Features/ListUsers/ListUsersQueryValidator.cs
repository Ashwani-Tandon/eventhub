// Registers the parameterless admin query with validation.
// The mediator runs these rules before dispatching the request to its handler.
using FluentValidation;

namespace Identity.Application.Features.ListUsers;

/// <summary>
/// Registers the parameterless admin query with validation. The mediator runs these rules before dispatching the request to its handler.
/// </summary>
public sealed class ListUsersQueryValidator : AbstractValidator<ListUsersQuery>;
