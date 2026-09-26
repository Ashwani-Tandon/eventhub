// Identifies the purchase whose cancellation the caller wants to begin or resume.
// The handler checks ownership before interpreting a completed or pending cancellation replay.
using Booking.Application.Contracts;
using EventHub.BuildingBlocks.Messaging;

namespace Booking.Application.Features.CancelBooking;

/// <summary>Requests cancellation of an owned booking or retries its unfinished seat release.</summary>
public sealed record CancelBookingCommand(int Id) : ICommand<BookingDto>;
