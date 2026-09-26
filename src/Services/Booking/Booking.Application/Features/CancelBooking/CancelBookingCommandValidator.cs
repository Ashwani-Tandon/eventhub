// Validates the route identifier before loading a cancellation target.
// Booking ownership and event timing are evaluated after the aggregate is loaded.
using FluentValidation;

namespace Booking.Application.Features.CancelBooking;

/// <summary>Rejects nonpositive identifiers before the cancellation handler queries persistence.</summary>
public sealed class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    /// <summary>Requires a persisted booking identifier.</summary>
    public CancelBookingCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
