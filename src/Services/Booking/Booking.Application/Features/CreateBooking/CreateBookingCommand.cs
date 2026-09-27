// Carries a logged-in caller's purchase choices and optional header identity through the mediator.
// Ownership comes from ICurrentUser; no client-supplied user or organizer identity is accepted.
using Booking.Application.Contracts;
using EventHub.BuildingBlocks.Messaging;

namespace Booking.Application.Features.CreateBooking;

/// <summary>Requests tickets for an event; the optional failure switch is available only in Development.</summary>
public sealed record CreateBookingCommand(
    int EventId,
    int Quantity,
    bool SimulatePaymentFailure = false,
    string? IdempotencyKey = null) : ICommand<BookingDto>;
