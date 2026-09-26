// Declares the validation boundary for the ListBookings query.
// A supplied lifecycle filter must be one of the supported booking states.
using Booking.Domain;
using FluentValidation;

namespace Booking.Application.Features.ListBookings;

/// <summary>Validates the optional status filter before the SQL read.</summary>
public sealed class ListBookingsQueryValidator : AbstractValidator<ListBookingsQuery>
{
    /// <summary>Allows no filter or one of Booking's supported lifecycle states.</summary>
    public ListBookingsQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => status is null || BookingStatus.IsKnown(status))
            .WithMessage("Choose Confirmed or Cancelled.");
    }
}
