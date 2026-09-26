// Registers Booking's five HTTP operations and their authentication policies.
// Parameter attributes identify Postman inputs; the mediator performs validation and workflow decisions.
using Booking.Application.Features.CancelBooking;
using Booking.Application.Features.CreateBooking;
using Booking.Application.Features.GetBookingStats;
using Booking.Application.Features.GetMyBookings;
using Booking.Application.Features.ListBookings;
using EventHub.BuildingBlocks.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api;

/// <summary>Thin HTTP boundary for purchase creation, owner cancellation, history, Admin listing, and statistics.</summary>
public static class BookingEndpoints
{
    /// <summary>Maps the five CQRS use cases; CancellationToken is framework request context, not a client or DI field.</summary>
    public static void MapBookingEndpoints(this WebApplication app)
    {
        // JSON body supplies purchase fields; ISender comes from DI; ASP.NET Core provides request cancellation.
        app.MapPost("/bookings", async (
            [FromBody] CreateBookingCommand command,
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            var location = result.IsSuccess ? $"/booking/bookings/{result.Value.Id}" : "/booking/bookings";
            return result.ToCreatedHttpResult(location);
        }).RequireAuthorization();

        app.MapGet("/bookings/mine", async (
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetMyBookingsQuery(), cancellationToken);
            return result.ToHttpResult();
        })
            .RequireAuthorization();

        app.MapPost("/bookings/{id:int}/cancel", async (
            [FromRoute] int id,
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CancelBookingCommand(id), cancellationToken);
            return result.ToHttpResult();
        })
            .RequireAuthorization();

        app.MapGet("/bookings", async (
            [FromQuery] string? status,
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListBookingsQuery(status), cancellationToken);
            return result.ToHttpResult();
        })
            .RequireAuthorization(AuthPolicies.Admin);

        app.MapGet("/bookings/stats", async (
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetBookingStatsQuery(), cancellationToken);
            return result.ToHttpResult();
        })
            .RequireAuthorization(AuthPolicies.Organizer);
    }
}
