// Updates an existing event after checking ownership and domain invariants.
// Its validator handles request shape while the aggregate protects booked capacity and changed dates.
using Catalog.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using FluentValidation;

namespace Catalog.Application.Features.UpdateEvent;

/// <summary>
/// Updates an existing event after checking ownership and domain invariants. Its validator handles request shape while the aggregate protects booked capacity and changed dates.
/// </summary>
public sealed class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    /// <summary>
    /// Checks editable field limits; the loaded event separately checks booked capacity and whether a changed date is in the future.
    /// </summary>
    public UpdateEventCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Category).Must(Categories.IsKnown).WithMessage("Choose a known category.");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 10000);
        RuleFor(x => x.RowVersion).NotEmpty().Must(IsBase64RowVersion)
            .WithMessage("RowVersion must be the base64 value returned by Catalog.");
    }

    /// <summary>Checks that the organizer sent a revision stamp in the format Catalog returned.</summary>
    private static bool IsBase64RowVersion(string value)
    {
        Span<byte> bytes = stackalloc byte[8];
        return Convert.TryFromBase64String(value, bytes, out var written) && written == 8;
    }
}
