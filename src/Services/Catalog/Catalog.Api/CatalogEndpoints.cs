// Declares Catalog's public event API and separately guarded internal reservation API.
// Endpoints only identify HTTP parameter sources, send CQRS requests, and map Results to responses.
using Catalog.Application.Features.CreateEvent;
using Catalog.Application.Features.DeleteEvent;
using Catalog.Application.Features.GetEventById;
using Catalog.Application.Features.GetMyEvents;
using Catalog.Application.Features.ReleaseReservation;
using Catalog.Application.Features.ReserveSeats;
using Catalog.Application.Features.SearchEvents;
using Catalog.Application.Features.UpdateEvent;
using EventHub.BuildingBlocks.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api;

/// <summary>
/// Declares Catalog's public event API and separately guarded internal reservation API. Endpoints only identify HTTP parameter sources, send CQRS requests, and map Results to responses.
/// </summary>
public static class CatalogEndpoints
{
    /// <summary>
    /// Registers six public event endpoints and two protected internal reservation endpoints.
    /// </summary>
    public static void MapCatalogEndpoints(this WebApplication app)
    {
        app.MapGet("/events", async ([FromQuery] string? search, [FromQuery] string? category,
            [FromQuery] string? city, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
            [FromQuery] decimal? maxPrice, [FromQuery] int? page, [FromQuery] int? pageSize,
            [FromServices] ISender sender, CancellationToken ct) =>
        {
            var query = new SearchEventsQuery(search, category, city, from, to, maxPrice,
                page ?? 1, pageSize ?? 12);
            return (await sender.Send(query, ct)).ToHttpResult();
        }).AllowAnonymous();

        app.MapGet("/events/mine", async ([FromServices] ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyEventsQuery(), ct)).ToHttpResult())
            .RequireAuthorization(AuthPolicies.Organizer);

        app.MapGet("/events/{id:int}", async ([FromRoute] int id, [FromServices] ISender sender,
            CancellationToken ct) => (await sender.Send(new GetEventByIdQuery(id), ct)).ToHttpResult())
            .AllowAnonymous();

        app.MapPost("/events", async ([FromBody] EventInput input, [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateEventCommand(input.Title, input.Description,
                input.Category, input.Venue, input.City, input.StartsAt, input.Price, input.Capacity), ct);
            return result.IsSuccess
                ? Results.Created($"/catalog/events/{result.Value.Id}", result.Value)
                : result.ToHttpResult();
        }).RequireAuthorization(AuthPolicies.Organizer);

        app.MapPut("/events/{id:int}", async ([FromRoute] int id, [FromBody] UpdateEventInput input,
            [FromServices] ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateEventCommand(id, input.Title, input.Description, input.Category,
                input.Venue, input.City, input.StartsAt, input.Price, input.Capacity, input.RowVersion), ct)).ToHttpResult())
            .RequireAuthorization(AuthPolicies.Organizer);

        app.MapDelete("/events/{id:int}", async ([FromRoute] int id, [FromServices] ISender sender,
            CancellationToken ct) => (await sender.Send(new DeleteEventCommand(id), ct)).ToHttpResult())
            .RequireAuthorization(AuthPolicies.Organizer);

        var internalApi = app.MapGroup("/internal").RequireAuthorization()
            .AddEndpointFilter<BookingCredentialFilter>();
        internalApi.MapPost("/events/{id:int}/reservations", async ([FromRoute] int id,
            [FromBody] ReserveSeatsInput input, [FromServices] ISender sender, CancellationToken ct) =>
            (await sender.Send(new ReserveSeatsCommand(id, input.ReservationId, input.Quantity), ct)).ToHttpResult());
        internalApi.MapPost("/reservations/{reservationId:guid}/release", async ([FromRoute] Guid reservationId,
            [FromServices] ISender sender, CancellationToken ct) =>
            (await sender.Send(new ReleaseReservationCommand(reservationId), ct)).ToHttpResult());

        // CancellationToken is supplied by ASP.NET Core for request cancellation; it is not Postman input or DI.
    }
}

/// <summary>
/// Client-editable event fields. Ownership and booked-seat counts are assigned by the server, never accepted from this body.
/// </summary>
public sealed record EventInput(
    string Title,
    string Description,
    string Category,
    string Venue,
    string City,
    DateTimeOffset StartsAt,
    decimal Price,
    int Capacity);

/// <summary>Editable event fields plus the version the organizer originally loaded.</summary>
public sealed record UpdateEventInput(
    string Title,
    string Description,
    string Category,
    string Venue,
    string City,
    DateTimeOffset StartsAt,
    decimal Price,
    int Capacity,
    string RowVersion);

/// <summary>
/// Booking's stable reservation identifier and requested ticket quantity; the user's identity comes from the validated JWT.
/// </summary>
public sealed record ReserveSeatsInput(
    Guid ReservationId,
    int Quantity);
