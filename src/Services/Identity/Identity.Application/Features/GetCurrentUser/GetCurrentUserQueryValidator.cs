// Registers the read request with validation; it has no input fields.
// The mediator runs these rules before dispatching the request to its handler.
using FluentValidation;

namespace Identity.Application.Features.GetCurrentUser;

/// <summary>
/// Registers the read request with validation; it has no input fields. The mediator runs these rules before dispatching the request to its handler.
/// </summary>
public sealed class GetCurrentUserQueryValidator : AbstractValidator<GetCurrentUserQuery>;
