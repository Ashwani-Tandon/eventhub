# Booking service — purpose and API reference

Booking turns an attendee's request into a confirmed ticket purchase and manages cancellation, history, and sales reporting.
This reference explains all five business operations, their inputs and responses, and the validation/error messages a caller can receive.

## What this service does

Booking owns `bookingdb`, including the purchaser, quantity, payment reference, reservation identity, and cancellation state.
It copies the event title, start time, organizer, and price at purchase time. Those snapshots let history and statistics work without contacting Catalog and retain the purchase-time facts after an event changes.

A keyed purchase first commits a `BookingRequest` claim, then runs: read the event → check its start time → reserve seats in Catalog → simulate payment → save a Confirmed booking → mark the claim Completed.
If payment or saving fails after reservation, Booking releases that reservation. This is compensation, not a transaction shared across databases.
Payment is fake: no money is charged. Approximately 90% of request identities succeed; the same `(userId, Idempotency-Key)` gives a stable simulated outcome and payment reference.

## APIs at a glance

Base URL: `http://localhost:5100/booking`. Every operation requires `Authorization: Bearer <accessToken>`.

| Method | Path                    | Purpose                                                | Who can call it                   | Success                |
| ------ | ----------------------- | ------------------------------------------------------ | --------------------------------- | ---------------------- |
| POST   | `/bookings`             | Purchase 1–10 tickets                                  | Any logged-in user                | 201, booking object    |
| GET    | `/bookings/mine`        | Read your booking history                              | Any logged-in user                | 200, booking array     |
| POST   | `/bookings/{id}/cancel` | Cancel your booking or finish its pending seat release | Booking owner, regardless of role | 200, booking object    |
| GET    | `/bookings`             | List all bookings, optionally filtered by status       | Admin                             | 200, booking array     |
| GET    | `/bookings/stats`       | Read scoped booking and sales statistics               | Organizer or Admin                | 200, statistics object |

Missing, expired, or invalid bearer tokens produce 401. A valid token without the required role produces 403.
These authentication/role failures occur before the handler and do not promise the business messages listed below.

## 1. Purchase tickets — POST `/bookings`

Send a JSON body; user identity and price are taken from trusted server data, not this body. For a retryable purchase, also send `Idempotency-Key: <opaque value>` (maximum 200 characters):

```json
{
    "eventId": 2,
    "quantity": 2,
    "simulatePaymentFailure": false
}
```

| Body field               | Meaning and rule                                                               |
| ------------------------ | ------------------------------------------------------------------------------ |
| `eventId`                | Catalog event ID; must be greater than zero and exist                          |
| `quantity`               | Ticket count; integer from 1 through 10, with enough available seats           |
| `simulatePaymentFailure` | Optional boolean, defaults to false; true forces a decline only in Development |

The event must not have started. Success returns 201, a booking object with `status: "Confirmed"`, and a Location header.
That Location identifies the created resource; there is currently no GET-by-booking-ID operation.

Repeating a key for the same event, quantity, and development failure switch returns the original 201 response and booking ID. A concurrent loser polls the durable claim for up to five seconds; it does not call Catalog or payment. Reusing the key with changed inputs returns 409 `Booking.IdempotencyMismatch`, `This idempotency key was already used with different booking details.` Omitting the header preserves the original behavior: every submission is a new purchase.

### Why the claim comes first

A unique index on the final Booking row is too late: two requests could both reserve and pay before either inserts that row. `BookingRequests` instead has a unique `(UserId, IdempotencyKey)` index and is committed before the first Catalog call. Its stored reservation GUID makes Catalog replay-safe, while the fake payment derives one result from the user and key. Booking also keeps a filtered unique `(UserId, IdempotencyKey)` index as a second database guard.

A Processing claim has a two-minute lease. A normal concurrent replay waits briefly; after the lease expires, one caller atomically takes it over and resumes with the stored identities. If a crash happened after the Booking row was saved but before the claim was marked Completed, recovery finds that row by reservation GUID and finishes the claim instead of repeating side effects. Expected 400/404/409/422 failures remove the claim; where a reservation exists, compensation must succeed before removal.

The remaining limitation is the unavoidable distributed crash window: Booking cannot atomically commit its SQL row together with Catalog or a real payment provider. Stable identities make retries convergent, but no background worker resumes abandoned claims; a caller must replay the same key. A lease takeover also assumes the original worker is no longer active after two minutes. This learning design is not a replacement for a payment provider's durable idempotency ledger or an outbox/saga.

| Failure                                            | HTTP | Code                             | Message received                                                                               |
| -------------------------------------------------- | ---- | -------------------------------- | ---------------------------------------------------------------------------------------------- |
| Nonpositive event ID                               | 400  | `Validation.Failed`              | `errors.EventId`: `'Event Id' must be greater than '0'.`                                       |
| Quantity outside 1–10                              | 400  | `Validation.Failed`              | `errors.Quantity`: `'Quantity' must be between 1 and 10. You entered {value}.`                 |
| Event missing                                      | 404  | `Booking.EventNotFound`          | `Event not found.`                                                                             |
| Event already started                              | 400  | `Booking.EventStarted`           | detail: `The event has already started.`; `errors.EventId`: `The event must not have started.` |
| Insufficient seats                                 | 409  | `Reservation.NotEnoughSeats`     | `Not enough seats are available.`                                                              |
| Key reused with different request details          | 409  | `Booking.IdempotencyMismatch`    | `This idempotency key was already used with different booking details.`                         |
| Payment declined and compensation succeeded        | 422  | `Booking.PaymentFailed`          | `Payment failed. Reserved seats have been released.`                                           |
| Catalog unreachable, timeout, or unusable response | 503  | `Booking.CatalogUnavailable`     | `Catalog service unavailable`                                                                  |
| Local save failed and compensation succeeded       | 503  | `Booking.PersistenceUnavailable` | `Booking database unavailable`                                                                 |

If releasing seats after a failure also fails, the dependency error is returned instead of claiming seats were released, and the claim remains for safe recovery. A matching replay that is still inside the active five-second observation window can receive 503 `Booking.RequestInProgress`; it should retry with the same key.
The Domain also protects quantity with `Booking.InvalidQuantity`: `Quantity must be between 1 and 10.`; normally the request validator rejects this first with the message above.

## 2. Your history — GET `/bookings/mine`

No request body or query parameters. Returns only the user identified by the bearer token, ordered by `createdAt` descending, then ID descending. No bookings returns `[]`.
Both Confirmed and Cancelled records are included, including pending seat releases. This reads Booking's own database and still works when Catalog is unavailable.

## 3. Cancel — POST `/bookings/{id}/cancel`

Supply a positive booking ID in the route. No JSON body is needed. Another user's booking cannot be cancelled, even by an Admin.
For a new cancellation, the booking must be Confirmed and its event must not have started.

Booking saves `status: "Cancelled"` and `seatReleasePending: true` before asking Catalog to release seats. On successful release, it saves `seatReleasePending: false` and returns 200.
If Catalog is unavailable, the request returns 503 but the cancellation remains saved. After Catalog recovers, repeat this same cancellation: it finishes the pending release without checking the start time again. Repeating a completed cancellation returns 200 without returning seats twice.
There is no background retry worker; a caller must retry pending cancellation.

| Failure                                        | HTTP | Code                             | Message received                                                                               |
| ---------------------------------------------- | ---- | -------------------------------- | ---------------------------------------------------------------------------------------------- |
| Nonpositive ID                                 | 400  | `Validation.Failed`              | `errors.Id`: `'Id' must be greater than '0'.`                                                  |
| Booking missing                                | 404  | `Booking.NotFound`               | `Booking not found.`                                                                           |
| Different owner                                | 403  | `Booking.Forbidden`              | `This booking belongs to another user.`                                                        |
| New cancellation is not Confirmed              | 400  | `Booking.NotConfirmed`           | `Only confirmed bookings can begin cancellation.`                                              |
| Event started before cancellation was accepted | 400  | `Booking.EventStarted`           | detail: `The event has already started.`; `errors.EventId`: `The event must not have started.` |
| Catalog unavailable during release             | 503  | `Booking.CatalogUnavailable`     | `Catalog service unavailable`                                                                  |
| Booking state could not be saved               | 503  | `Booking.PersistenceUnavailable` | `Booking database unavailable`                                                                 |

Cancelled records are not deleted. If the final flag-clear save fails after seats were returned, replay is safe because Catalog releases the reservation only once.

## 4. Admin listing — GET `/bookings`

No body. Omit `status` to return all bookings, or use `/bookings?status=Confirmed` or `/bookings?status=Cancelled`.
Values are case-sensitive. An invalid or empty supplied status produces 400, code `Validation.Failed`, with `errors.Status`: `Choose Confirmed or Cancelled.`
Results are ordered newest first, include every user's bookings, and are not paginated. Empty results return `[]`.

## 5. Sales statistics — GET `/bookings/stats`

No body or filters. Organizer sees only bookings whose stored `organizerId` matches their token; Admin sees all organizers.
Attendee receives 403. This operation reads only Booking's local snapshots, not Catalog.

| Response section | Fields and meaning                                                                                                                                |
| ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| `totals`         | `revenue` and `ticketsSold` count Confirmed purchases only; `bookings` counts all records; `cancelled` counts Cancelled records                   |
| `revenueByMonth` | Six calendar months, current UTC month plus previous five; each entry has `month` (`YYYY-MM`) and Confirmed `revenue`; missing sales produce zero |
| `topEvents`      | Up to ten events, ranked by Confirmed tickets descending then event ID; fields `eventId`, `title`, `tickets`, `revenue`                           |
| `statusCounts`   | Two entries with `status` and booking `count`: Confirmed and Cancelled                                                                            |

Totals and top events use all stored history, not just the six-month chart window. Pending cancellations are Cancelled, so they are excluded from revenue and tickets.

## Booking response fields

All purchase, cancellation, and listing responses use the same object:

| Fields                                                  | Meaning                                                                           |
| ------------------------------------------------------- | --------------------------------------------------------------------------------- |
| `id`, `userId`                                          | Booking ID and purchaser identity                                                 |
| `eventId`, `eventTitle`, `eventStartsAt`, `organizerId` | Event identity and copied purchase-time facts                                     |
| `quantity`, `unitPrice`, `total`                        | Tickets, copied price per ticket, and quantity × price                            |
| `status`                                                | `Confirmed` or `Cancelled`                                                        |
| `paymentRef`, `reservationId`                           | Fake payment reference and Catalog reservation GUID                               |
| `seatReleasePending`                                    | True means cancellation was saved but release completion still needs confirmation |
| `createdAt`                                             | Purchase record timestamp; cancellation does not change its ordering timestamp    |

## Reading errors and service boundaries

Handler errors use ProblemDetails: `status`, `title`, `detail`, `code`, and `traceId`; field validation adds an `errors` dictionary.
Request-validation errors have title `Validation failed`, code `Validation.Failed`, and detail `One or more validation errors occurred.`
For example, quantity 11 produces:

```json
{
    "title": "Validation failed",
    "status": 400,
    "detail": "One or more validation errors occurred.",
    "errors": {
        "Quantity": ["'Quantity' must be between 1 and 10. You entered 11."]
    },
    "code": "Validation.Failed"
}
```

The table's `{value}` placeholders represent the submitted value, not literal response text. Malformed JSON, wrong parameter types, and route mismatches are handled by ASP.NET Core rather than these validators; their body/message may differ.
If a handler has no usable user identity, its explicit error is 401, `Booking.Unauthenticated`, `Authentication is required.`
Booking preserves Catalog's expected validation, 401, 403, and 409 errors. Every Booking 503 supplies `Retry-After: 5`; this suggests when to try again, not an automatic retry or a promise that the service has recovered.

Booking forwards the caller's bearer token to Catalog and adds `X-EventHub-Service` only for internal reserve/release operations. The token identifies the attendee; the separate credential proves that Booking is making the seat change. Neither service opens the other's database.
Outbound Catalog calls use the shared resilience pipeline: a ten-second total limit contains safe retries, a circuit breaker, and a two-second limit for each attempt. Event reads retry because GET is safe. Reserve and release POSTs retry because their stable `reservationId` makes Catalog deduplicate them; unrelated writes are never made retryable by accident. Once failures open the circuit, Booking returns the same 503 immediately instead of waiting on a network call. Retry and breaker events include the distributed trace ID in Aspire logs.

Operational endpoints `/health` and `/alive` are separate from these five business operations. Normal examples are in [booking.http](../../../src/Services/Booking/Booking.Api/booking.http), and the stop/restart proof is in [resilience.http](../../../src/Services/Booking/Booking.Api/resilience.http). The [Catalog reference](../catalog/API.md) explains the seat APIs it calls.
