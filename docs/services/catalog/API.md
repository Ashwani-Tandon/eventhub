# Catalog service — purpose and API reference

Catalog owns the events people browse and the capacity that prevents tickets from being oversold.
This reference explains its six public-facing business APIs and two protected internal APIs, including inputs, results, access rules, and validation/error messages.

## What this service does

Catalog owns `catalogdb`: event details, organizer ownership, capacity, booked-seat totals, and seat reservations.
Guests browse events. Organizers create and manage their own events; Admin can manage any organizer's events.
Booking uses Catalog's internal APIs to hold and return seats. Catalog does not charge payments or create Booking records.
Seat changes and reservation state are saved atomically in Catalog's own database, protecting capacity and preventing duplicate changes when a reservation call is repeated.

## APIs at a glance

Public base URL: `http://localhost:5100/catalog`. Protected calls require `Authorization: Bearer <accessToken>`.

| Method | Path                                             | Purpose                                | Access                           | Success                 |
| ------ | ------------------------------------------------ | -------------------------------------- | -------------------------------- | ----------------------- |
| GET    | `/events`                                        | Search and page events                 | Anonymous allowed                | 200, `{ items, total }` |
| GET    | `/events/{id}`                                   | Read one event                         | Anonymous allowed                | 200, event object       |
| GET    | `/events/mine`                                   | Read your managed events               | Organizer; Admin gets all        | 200, event array        |
| POST   | `/events`                                        | Create an event owned by the caller    | Organizer or Admin               | 201, event object       |
| PUT    | `/events/{id}`                                   | Replace editable event details         | Owning Organizer or Admin        | 200, event object       |
| DELETE | `/events/{id}`                                   | Delete an event with no booked seats   | Owning Organizer or Admin        | 204, no body            |
| POST   | `/internal/events/{id}/reservations`             | Hold seats under a reservation GUID    | Booking service plus user JWT    | 200, reservation object |
| POST   | `/internal/reservations/{reservationId}/release` | Return a held reservation's seats once | Booking service plus owner's JWT | 200, reservation object |

The two `/internal` paths are called on Catalog's direct Aspire-discovered address, **not through the gateway**. Their gateway equivalents are excluded and return 404.
Missing/invalid/expired JWT yields 401; insufficient role yields 403 before business logic. These middleware responses do not promise the handler messages below.

## 1. Search events — GET `/events`

All query parameters are optional; no request body:

| Query parameter | Meaning and rule                                                                                           |
| --------------- | ---------------------------------------------------------------------------------------------------------- |
| `search`        | Trimmed substring matched against title or description; blank means no text filter                         |
| `category`      | One of `Music`, `Tech`, `Sports`, `Comedy`, `Workshop`, case-sensitive                                     |
| `city`          | City equality filter; blank means no city filter                                                           |
| `from`          | Inclusive start-time lower bound as an offset timestamp; omitted means current time, excluding past events |
| `to`            | Inclusive start-time upper bound; cannot precede supplied `from`                                           |
| `maxPrice`      | Inclusive price ceiling; zero or greater                                                                   |
| `page`          | One-based page number; defaults to 1                                                                       |
| `pageSize`      | 1–50; defaults to 12                                                                                       |

Example: `/events?category=Tech&maxPrice=500&page=1&pageSize=12`.
Results are ordered by start time. `items` contains the requested page; `total` counts all matching events before paging.
No matches returns `{ "items": [], "total": 0 }`, not 404. Supplying an earlier `from` allows historical events.

| Invalid input          | 400 field message (`Validation.Failed`)                                                                   |
| ---------------------- | --------------------------------------------------------------------------------------------------------- |
| Unknown category       | `Category`: `Choose a known category.`                                                                    |
| Negative maximum price | `MaxPrice`: `'Max Price' must be greater than or equal to '0'.`                                           |
| Page zero or negative  | `Page`: `'Page' must be greater than '0'.`                                                                |
| Page size outside 1–50 | `PageSize`: `'Page Size' must be between 1 and 50. You entered {value}.`                                  |
| `to` before `from`     | `To`: `'To' must be greater than or equal to '{from}'.` (the supplied date is formatted by the validator) |

## 2. Event details — GET `/events/{id}`

Supply a positive integer route ID. Returns one event, including past events and sold-out events.
A nonpositive ID yields 400 with `errors.Id`: `'Id' must be greater than '0'.`; a missing event yields 404, `Event.NotFound`, `Event not found.`

## 3. Managed events — GET `/events/mine`

No body or query parameters. Organizer receives only events whose `organizerId` matches their token; Admin receives all events.
Returns an array ordered by start time, including historical events; no events returns `[]`. Attendee receives 403.

## 4. Create an event — POST `/events`

Send these editable fields in a JSON body:

```json
{
    "title": "Community Tech Meetup",
    "description": "An evening of talks and discussion.",
    "category": "Tech",
    "venue": "Community Hall",
    "city": "Mumbai",
    "startsAt": "2027-12-01T18:00:00+05:30",
    "price": 250,
    "capacity": 100
}
```

Choose a future date when trying the example. The caller becomes the organizer, even when the caller is an Admin.
The server assigns ID and ownership and starts `seatsBooked` at zero. Success returns 201, the event object, and Location `/catalog/events/{id}`.

| Field           | Rule                                                                                     | 400 field message (`Validation.Failed`)                                                                                 |
| --------------- | ---------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `title`         | Not empty/whitespace; maximum 120 characters                                             | `'Title' must not be empty.`; `The length of 'Title' must be 120 characters or fewer. You entered {length} characters.` |
| `description`   | Maximum 2000 characters                                                                  | `The length of 'Description' must be 2000 characters or fewer. You entered {length} characters.`                        |
| `category`      | Known category from search's list                                                        | `Choose a known category.`                                                                                              |
| `startsAt`      | Later than the current time                                                              | `Start time must be in the future.`                                                                                     |
| `price`         | Zero or greater                                                                          | `'Price' must be greater than or equal to '0'.`                                                                         |
| `capacity`      | Integer from 1 through 10000                                                             | `'Capacity' must be between 1 and 10000. You entered {value}.`                                                          |
| `venue`, `city` | Text fields; current application validator has no explicit required/length rule for them | No custom field-validation message is defined                                                                           |

Ownership, `seatsBooked`, and `seatsLeft` are not accepted as editable fields. Do not treat the absence of a validator as a guarantee that arbitrary nulls or oversized text will persist: SQL constraints and JSON binding still apply.

## 5. Update an event — PUT `/events/{id}`

Supply a positive route ID and the same complete JSON body as creation, plus the `rowVersion` base64 string returned when the event was read. This is replacement of editable fields, not a partial PATCH.
Title, description, category, price, and capacity use the same validation rules/messages as creation; invalid route ID uses `'Id' must be greater than '0'.`
An Organizer may update only their own event; Admin may update any event.

The Domain additionally requires capacity to be at least the already booked quantity. If start time changes, the new time must be in the future; an unchanged historical start time is allowed.
Success returns the updated event with existing booked seats retained. Existing Booking snapshots are not rewritten by an event edit.

| Failure                          | HTTP | Code              | Message                                                                                              |
| -------------------------------- | ---- | ----------------- | ---------------------------------------------------------------------------------------------------- |
| Event missing                    | 404  | `Event.NotFound`  | `Event not found.`                                                                                   |
| Different organizer (not Admin)  | 403  | `Event.Forbidden` | `You do not own this event.`                                                                         |
| Capacity below booked seats      | 400  | `Event.Invalid`   | detail: `The event is invalid.`; `errors.Capacity`: `Capacity cannot be below seats already booked.` |
| Changed start time not in future | 400  | `Event.Invalid`   | detail: `The event is invalid.`; `errors.StartsAt`: `Start time must be in the future.`              |
| Missing or malformed row version | 400  | `Validation.Failed` | `RowVersion must be the base64 value returned by Catalog.`                                        |
| Event changed since it was read   | 409  | `Event.ConcurrencyConflict` | `This event was changed by someone else — reload and try again.`                            |

SQL Server updates the event's eight-byte `rowversion` whenever the row changes, including seat-count changes. Catalog tells EF Core that the submitted value is the original version; if the database now has another version, `DbUpdateConcurrencyException` becomes the expected 409 above. The response carries the new version, so the next edit must use that value. This is optimistic concurrency: it avoids long database locks but deliberately asks a stale editor to reload and reconcile.

## 6. Delete an event — DELETE `/events/{id}`

Positive route ID, no body. Only the owning Organizer or Admin may delete it, and `seatsBooked` must be zero.
Success returns 204 with no body. The event disappears from subsequent reads.

| Failure                         | HTTP | Code                | Message                                         |
| ------------------------------- | ---- | ------------------- | ----------------------------------------------- |
| Nonpositive ID                  | 400  | `Validation.Failed` | `errors.Id`: `'Id' must be greater than '0'.`   |
| Event missing                   | 404  | `Event.NotFound`    | `Event not found.`                              |
| Different organizer (not Admin) | 403  | `Event.Forbidden`   | `You do not own this event.`                    |
| Any booked seats remain         | 409  | `Event.HasBookings` | `An event with booked seats cannot be deleted.` |

## 7. Internal hold — POST `/internal/events/{id}/reservations`

Called by Booking on the direct Catalog service URL. Supply a user bearer token, `X-EventHub-Service: <Booking service credential>`, a positive event route ID, and this body:

```json
{
    "reservationId": "a53c5842-74b8-449d-bf72-eeb68b8567dd",
    "quantity": 2
}
```

`reservationId` must be a nonempty GUID; `quantity` must be positive. The internal validator does not impose Booking's 10-ticket limit.
Success increases `seatsBooked` by quantity and returns `{ "reservationId": "...", "status": "Held" }`.
Repeating the same GUID with the same event, user, and quantity returns its stored state without another seat change. If that reservation was already Released, replay returns Released without holding it again.

| Failure                                             | HTTP | Code                         | Message                                                        |
| --------------------------------------------------- | ---- | ---------------------------- | -------------------------------------------------------------- |
| Nonpositive event ID                                | 400  | `Validation.Failed`          | `errors.EventId`: `'Event Id' must be greater than '0'.`       |
| Empty reservation GUID                              | 400  | `Validation.Failed`          | `errors.ReservationId`: `'Reservation Id' must not be empty.`  |
| Nonpositive quantity                                | 400  | `Validation.Failed`          | `errors.Quantity`: `'Quantity' must be greater than '0'.`      |
| Event missing                                       | 404  | `Event.NotFound`             | `Event not found.`                                             |
| Insufficient available seats                        | 409  | `Reservation.NotEnoughSeats` | `Not enough seats are available.`                              |
| GUID reused with different event, user, or quantity | 409  | `Reservation.ReplayMismatch` | `This reservation id was already used with different details.` |

This internal hold checks capacity, not the event start time; Booking checks timing before calling it.

## 8. Internal release — POST `/internal/reservations/{reservationId}/release`

Same two credentials as the hold. Supply a nonempty GUID route value; no body.
For a Held reservation owned by the bearer-token user, Catalog returns its quantity to available seats and marks it Released atomically.
Success returns `{ "reservationId": "...", "status": "Released" }`. An already Released or unknown GUID also returns 200 Released without changing seats, making release safe to repeat.
An empty GUID yields 400, `Validation.Failed`, `errors.ReservationId`: `'Reservation Id' must not be empty.`
A different owner's reservation yields 403, `Reservation.Forbidden`, `This reservation belongs to another user.`

### Why internal operations need two credentials

The user JWT identifies the reservation owner. The service credential proves that trusted Booking—not an attendee bypassing payment—is changing seats.
Both are required; removing the routes from the gateway is an additional exposure restriction, not authentication.
Missing/wrong service credential with an otherwise valid JWT returns 403 ProblemDetails, title `Forbidden`, detail `A valid Booking service credential is required.`; this filter does not add a domain error code.
The credential is supplied by configuration and must not be copied into committed examples.

## Event response fields

| Fields                                              | Meaning                                                       |
| --------------------------------------------------- | ------------------------------------------------------------- |
| `id`, `organizerId`                                 | Event ID and organizer's user GUID                            |
| `title`, `description`, `category`, `venue`, `city` | Browse/display details                                        |
| `startsAt`                                          | Start timestamp with offset                                   |
| `price`                                             | Price per ticket; zero means free                             |
| `capacity`                                          | Maximum seats                                                 |
| `seatsBooked`                                       | Seats currently held/booked through reservations              |
| `seatsLeft`                                         | Calculated as capacity minus seatsBooked; zero means sold out |
| `rowVersion`                                        | Base64 optimistic-concurrency token required by the next PUT  |

## Reading errors and domain safeguards

Handler errors use ProblemDetails with `status`, `title`, `detail`, `code`, and `traceId`; validation adds `errors`, keyed by the command/query's C# field name (`Title`, `PageSize`, etc.).
Request validation has title `Validation failed`, code `Validation.Failed`, and detail `One or more validation errors occurred.` Multiple field failures may be returned together.
Placeholders `{value}`, `{length}`, and `{from}` in the tables are substituted values, not literal text.
Malformed JSON, invalid date/number types, and routes that do not match the `int`/`guid` constraint are framework failures and may use a different response body.

The Domain independently guards event invariants. Its `Event.Invalid` detail is `The event is invalid.` with one of these field messages, even though normal invalid HTTP input is usually rejected by the validators first:

| Field      | Domain message                                                                              |
| ---------- | ------------------------------------------------------------------------------------------- |
| `Title`    | `Title is required.`                                                                        |
| `Category` | `Choose a known category.`                                                                  |
| `Price`    | `Price must be zero or greater.`                                                            |
| `Capacity` | `Capacity must be between 1 and 10000.` or `Capacity cannot be below seats already booked.` |
| `StartsAt` | `Start time must be in the future.`                                                         |

If a handler has no usable caller identity, it returns 401, `Event.Unauthenticated`, `Authentication is required.` Unexpected exceptions use the common safe 500 response; they are not field-validation messages.
Operational `/health` and `/alive` endpoints are separate from the eight business operations.

Example requests are in [catalog.http](../../../src/Services/Catalog/Catalog.Api/catalog.http). The [Booking reference](../booking/API.md) explains purchase and cancellation workflows that call these internal APIs.
