// Declares the validation boundary for the GetBookingStats query.
// This query has no client fields; authentication and role policies establish its caller.
using FluentValidation;

namespace Booking.Application.Features.GetBookingStats;

/// <summary>Keeps this fieldless query in the common validation pipeline.</summary>
public sealed class GetBookingStatsQueryValidator : AbstractValidator<GetBookingStatsQuery>
{

}
