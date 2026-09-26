# Catalog API and code guide

Catalog owns events and seat availability. This guide explains its eight business endpoints and how requests move through the code.
Use the public gateway URLs in Postman; internal URLs are called by Booking using Catalog's Aspire-assigned address.

| Method | URL                                                                 | Operation                                | Access                                       |
| ------ | ------------------------------------------------------------------- | ---------------------------------------- | -------------------------------------------- |
| GET    | `http://localhost:5100/catalog/events`                              | Search and page upcoming events          | Public                                       |
| GET    | `http://localhost:5100/catalog/events/{id}`                         | Read one event and its seats left        | Public                                       |
| GET    | `http://localhost:5100/catalog/events/mine`                         | Read owned events, including past events | Organizer; Admin sees all                    |
| POST   | `http://localhost:5100/catalog/events`                              | Create an event owned by the caller      | Organizer or Admin                           |
| PUT    | `http://localhost:5100/catalog/events/{id}`                         | Update event fields                      | Owner or Admin                               |
| DELETE | `http://localhost:5100/catalog/events/{id}`                         | Delete an event with no booked seats     | Owner or Admin                               |
| POST   | `{catalogServiceUrl}/internal/events/{id}/reservations`             | Hold seats once per reservation id       | User JWT and Booking credential              |
| POST   | `{catalogServiceUrl}/internal/reservations/{reservationId}/release` | Return held seats once                   | Reservation owner JWT and Booking credential |

There are six public operations and two internal operations. These are Minimal API endpoint delegates, the equivalent of controller action methods. `/catalog/health` is an additional infrastructure health endpoint.

Search query fields are `search`, `category`, `city`, `from`, `to`, `maxPrice`, `page` (default 1), and `pageSize` (default 12, maximum 50). The response contains `items` and the total matching count before paging.

Create and update accept this JSON body:

```json
{
    "title": "Learning workshop",
    "description": "An introduction to event booking",
    "category": "Workshop",
    "venue": "Learning Lab",
    "city": "Delhi",
    "startsAt": "2027-12-01T10:00:00Z",
    "price": 250,
    "capacity": 25
}
```

Reserve accepts `{ "reservationId": "a GUID", "quantity": 2 }`. Release has no body. Protected requests use `Authorization: Bearer <token>`. Internal requests also supply `X-EventHub-Service: <Booking credential>`; retrieve that credential locally from AppHost user-secrets and never commit it.

## Why the Booking credential filter exists

A JWT answers “which user is this?” An attendee has a valid JWT, but must use Booking to purchase tickets. If the JWT alone allowed seat reservation, that attendee could call Catalog directly, hold seats without creating a booking or going through payment, and consume availability.

`BookingCredentialFilter` checks an additional secret that AppHost supplies to Booking and Catalog. It answers “does this caller possess Booking's service credential?” A missing or incorrect credential returns 403 before the reservation handler runs. A valid credential continues to the endpoint. JWT authentication still runs first and supplies the user identity for reservation ownership.

The filter hashes both supplied and expected values into equal-length byte arrays, then compares them with `FixedTimeEquals`. This avoids stopping the comparison at the first different secret byte. Hashing here supports comparison; the secret is still stored in user-secrets, and the comparison is not password storage.

YARP excludes `/internal`, so public gateway calls return 404. Direct calls to Catalog remain possible in the local environment, which is why excluding a gateway route alone does not establish the caller's identity. This follows SPEC AR-09.

## How to read a feature

Each feature folder has three separate files. The command or query describes its input, the validator checks input fields, and the handler carries out that particular operation. The mediator runs logging, validation, and performance behaviors before the handler.

For creation, the endpoint reads the body and sends `CreateEventCommand`. Its validator checks the fields. Its handler reads the authenticated user, calls `Event.Create`, saves through the unit of work, and returns an `EventDto`. The endpoint maps that successful result to 201.

For reservation, the handler calls `IEventRepository.TryReserveAsync`. Infrastructure checks for a replay, conditionally increments seats only if capacity permits, and inserts the reservation in the same transaction. A losing duplicate insert rolls back its seat increment and returns the winning reservation when the inputs match.

`[FromBody]`, `[FromRoute]`, and `[FromQuery]` identify Postman inputs; `[FromServices]` identifies dependency injection. `CancellationToken` is supplied by ASP.NET Core when the request is cancelled and is not a client field.

## Formatting

Run `dotnet format whitespace EventHub.sln --no-restore` for C# and supported workspace files, using `.editorconfig`. Prettier formats JSON and Markdown; its standard parser does not format C#. Generated build outputs and migrations stay under their generators' control.
