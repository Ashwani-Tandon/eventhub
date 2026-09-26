// Rejects invalid purchase input before Catalog is called.
// Quantity limits protect the use case independently of the capacity check performed by Catalog.
using FluentValidation;

namespace Booking.Application.Features.CreateBooking;

/// <summary>Validates the event identifier and the purchase quantity.</summary>
public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    /// <summary>Requires a valid event id and between one and ten tickets.</summary>
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.EventId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 10);
    }
}
