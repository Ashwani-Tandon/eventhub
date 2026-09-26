// Carries event creation through validation into the domain aggregate.
// The handler assigns ownership from the authenticated user rather than trusting request data.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.CreateEvent;

/// <summary>
/// Carries event creation through validation into the domain aggregate. The handler assigns ownership from the authenticated user rather than trusting request data.
/// </summary>
public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    /// <summary>
    /// Requires a title, known category, nonnegative price, supported capacity, and a future start time before creation runs.
    /// </summary>
    public CreateEventCommandValidator(TimeProvider clock)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Category).Must(Categories.IsKnown).WithMessage("Choose a known category.");
        RuleFor(x => x.StartsAt).GreaterThan(_ => clock.GetUtcNow()).WithMessage("Start time must be in the future.");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 10000);
    }
}
