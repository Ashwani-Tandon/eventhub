// Declares the validation boundary for the GetMyBookings query.
// This query has no client fields; authentication and role policies establish its caller.
using FluentValidation;

namespace Booking.Application.Features.GetMyBookings;

/// <summary>Keeps this fieldless query in the common validation pipeline.</summary>
public sealed class GetMyBookingsQueryValidator : AbstractValidator<GetMyBookingsQuery>
{

}
